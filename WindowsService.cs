﻿using DICOMCapacitorWarden.util;
using log4net;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using Usb.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static Warden.util.VerifyDetachedSignature;

namespace DICOMCapacitorWarden
{
  public partial class WindowsService : ServiceBase
  {
    private static readonly ILog Logger = LogManager.GetLogger("WardenLog");

    private static readonly string HashLog = Path.Combine(Globals.LogDirPath, "hash.log");

    private static readonly string ClientLog = Path.Combine(Globals.LogDirPath, "update.log");

    public static string HashLogText = null;

    private bool QUITTING => false;

#if RELEASE
    private SpeechSynthesizer synth => new SpeechSynthesizer();
#endif

    public WindowsService()
    {
      InitializeComponent();
    }

    internal void TestStartupAndStop(string[] args)
    {
      OnStart(args);
      while (!QUITTING) { }
      OnStop();
    }

    protected override void OnStart(string[] args)
    {
      var usbEventWatcher = new UsbEventWatcher();

      usbEventWatcher.UsbDriveEjected += (_, path) => OnUsbDriveEjected(path);
      usbEventWatcher.UsbDriveMounted += (_, path) => OnUsbDriveMounted(path);

    }

    protected override void OnStop()
    {
      Logger.Info("Warden Terminating.");
    }

    private static bool FileVerification(FileInfo file)
    {
      if (File.Exists($"{file.FullName}.sig"))
      {
        FileInfo fileInfo = new FileInfo(file.FullName);
        FileInfo signature = new FileInfo($"{file.FullName}.sig");
        Logger.Info($"Verifying {file.FullName} against signature {signature.FullName}");

        // In this method we can just sign .zips using pgp.
        // If the .zip is modified the signature should fail
        // giving us checksums for free too. 

        if (VerifySignature(fileInfo.FullName, signature.FullName))
        {
          Logger.Info($"{file.FullName}: VERIFIED");
          return true;
        }
        Logger.Info($"{file.FullName}: UNABLE TO VERIFY");
      }
      return false;
    }

    private void ExtractFile(FileInfo file)
    {
      Logger.Info($"Extracting {file.FullName} to {file.DirectoryName}");

      try
      {
        ZipFile.ExtractToDirectory(file.FullName, file.DirectoryName);
      }
      catch (Exception ex)
      {
        Logger.Error(ex);
        return;
      }

      Logger.Info($"{file.FullName} extracted successfully.");
    }

    private void LoggerWithRobot(string strung)
    {
      Logger.Info(strung);

#if RELEASE
      synth.Speak(strung);
#endif

    }

    private void ExecutableOperation(Manifest manifest, DirectoryInfo directory)
    {
      var proc = new Process();
      Logger.Info($"Starting {manifest.Executable} with args {manifest.Args}");

      proc.StartInfo.WorkingDirectory = directory.FullName;
      proc.StartInfo.FileName = Path.Combine(directory.FullName, manifest.Executable);
      proc.StartInfo.UseShellExecute = false;
      proc.StartInfo.RedirectStandardOutput = true;

      if (!String.IsNullOrEmpty(manifest.Args))
        proc.StartInfo.Arguments = manifest.Args;

      proc.Start();

      var output = proc.StandardOutput.ReadToEnd();
      proc.WaitForExit();

      if (!String.IsNullOrEmpty(output)) Logger.Info(output);
      Logger.Info("Executable Operation: Success");
    }

    private void FileCopyOperation(Manifest manifest, DirectoryInfo directory)
    {
      if (FileCopy.CopyFolderContents(directory.FullName, @"C:\\"))
      {
        Logger.Info("Deleting Manifest File");
        File.Delete(@"C:\manifest.yml");
        Logger.Info("FileCopy Operation: Success");
      }
    }
    private bool ManifestExists(DirectoryInfo directory)
    {
      var manifest = directory.GetFiles("manifest.yml")[0];
      return File.Exists(manifest.FullName);
    }

    private Manifest OpenManifest(DirectoryInfo directory)
    {
      var manifest = directory.GetFiles("manifest.yml")[0];
      var manifestText = File.ReadAllText(manifest.FullName);
      var deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

      return deserializer.Deserialize<Manifest>(manifestText);
    }

    private void ProcessManifest(Manifest manifest, DirectoryInfo directory)
    {
      switch (manifest.Operation.ToLower())
      {
        case "executable":
          Logger.Info("Running Executable Operation from Manifest...");
          ExecutableOperation(manifest, directory);
          break;

        case "filecopy":
          Logger.Info("Running File Copy from Manifest...");
          FileCopyOperation(manifest, directory);
          break;

        default:
          Logger.Info("Manifest doesn't contain a valid operation.");
          break;
      }
    }

    private string StripHashCode(string filename)
    {
      var match = Regex.Match(filename, "^WARDEN-(.*).zip");
      return match.Groups[1].Value;
    }

    private void LogHashCode(string hashcode)
    {
      File.AppendAllText(HashLog, hashcode + Environment.NewLine);
      HashLogText = null;
    }

    private bool UpdateAlreadyProcessed(string hashCode)
    {
      if (!File.Exists(HashLog)) File.Create(HashLog);

      if (HashLogText == null) HashLogText = File.ReadAllText(HashLog);

      if (HashLogText.Contains(hashCode + Environment.NewLine))
      {
        Logger.Info($"{hashCode} has already been processed.");

        return true;
      }

      return false;
    }

    private void CleanupExtractedFiles(DirectoryInfo dir)
    {
      dir.Delete(true);
    }

    private DirectoryInfo GenerateReturnDirectory(FileInfo file)
    {
      return Directory.CreateDirectory(Path.Combine(file.DirectoryName,
        "log-" + Path.GetFileNameWithoutExtension(file.FullName)));
    }

    private void ReturnManifestDefinedFiles(Manifest manifest, FileInfo file, DirectoryInfo payloadDir)
    {
      DirectoryInfo returndir = GenerateReturnDirectory(file);
      var returnFile = payloadDir.GetFiles(manifest.ReturnFile)[0];

      if (returnFile.Exists)
      {
        returnFile.CopyTo(returndir.FullName + $"\\{returnFile.Name}", true);
        Logger.Info($"Copying requested return file {returnFile.Name} to {returndir.Name}");
        return;
      }
      Logger.Info($"Warden couldn't find requested return file {returnFile.Name}");
    }

    private void ReturnLogFile(FileInfo file)
    {
      FileInfo log = new FileInfo(ClientLog);

      DirectoryInfo returnDir = GenerateReturnDirectory(file);

      log.CopyTo(returnDir.FullName + $"\\{log.Name}", true);

      if (File.Exists(returnDir.FullName + ".zip"))
        File.Delete(returnDir.FullName + ".zip");

      ZipFile.CreateFromDirectory(returnDir.FullName, returnDir.FullName + ".zip");

      returnDir.Delete(true);
    }

    private void FlushClientLog()
    {
      FileInfo log = new FileInfo(ClientLog);
      File.Delete(log.FullName);
      File.WriteAllText(log.FullName, "");
    }

    private void OnUsbDriveMounted(string path)
    {
      Logger.Info($"{path} was mounted.  Searching for Warden Files...");
      DirectoryInfo dir = Directory.CreateDirectory(path);

      if (dir.Exists)
      {
        var files = dir.GetFiles("WARDEN*.zip");

        foreach (var file in files)
        {
          FlushClientLog();
          if (UpdateAlreadyProcessed(StripHashCode(file.Name)))
          {
            ReturnLogFile(file);
            continue;
          }

          LoggerWithRobot("Beginning Warden Update");

          ExtractFile(file);
          DirectoryInfo extractDir =
            Directory.CreateDirectory(Path.Combine(file.DirectoryName, Path.GetFileNameWithoutExtension(file.FullName)));

          if (extractDir.Exists && FileVerification(extractDir.GetFiles("payload.zip")[0]))
          {

            Logger.Info($"Processing payload in {extractDir.FullName}");
            ExtractFile(extractDir.GetFiles("payload.zip")[0]);

            var payloadDir = Directory.CreateDirectory(Path.Combine(extractDir.FullName, "payload"));

            if (ManifestExists(payloadDir))
            {
              Logger.Info("Opening Manifest...");
              var manifest = OpenManifest(payloadDir);

              ProcessManifest(manifest, payloadDir);
              LoggerWithRobot("Warden Update Completed");
              ReturnManifestDefinedFiles(manifest, file, payloadDir);
              ReturnLogFile(file);
              CleanupExtractedFiles(extractDir);
              LogHashCode(StripHashCode(file.Name));
              continue;
            }

            LoggerWithRobot("Warden Update Failed");
            ReturnLogFile(file);
            CleanupExtractedFiles(extractDir);
          }
        }
        return;
      }
      Logger.Error($"{dir.FullName} does not exist.");
    }

    private void OnUsbDriveEjected(string path)
    {
      Logger.Info($"{path} was ejected.");
    }
  }
}
