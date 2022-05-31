using DICOMCapacitorWarden.util;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using Usb.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static DICOMCapacitorWarden.util.VerifyDetachedSignature;

namespace DICOMCapacitorWarden
{
  public partial class WindowsService : ServiceBase
  {
    private static readonly ILog Logger = LogManager.GetLogger("WardenLog");
    private static readonly string HashLog = Path.Combine(Globals.LogDirPath, "hash.log");
    private static readonly string ClientLog = Path.Combine(Globals.LogDirPath, "update.log");
    public static string HashLogText = null;
    public static int UpdateErrors = 0;
    private bool QUITTING => false;

    private static List<(string, string, string)> CommandSuffixSubstitution =
      new List<(string, string, string)>
      {
        // extension | substitution | arguments-to-substitution
        ("bat", "cmd.exe", "/c" ),
        ("ps1", "powershell.exe", "-File")
      };

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
      Logger.Info("Starting Warden...");
      var usbEventWatcher = new UsbEventWatcher();

      usbEventWatcher.UsbDriveEjected += (_, path) => OnUsbDriveEjected(path);
      usbEventWatcher.UsbDriveMounted += (_, path) => OnUsbDriveMounted(path);

    }

    protected override void OnStop()
    {
      Logger.Info("Warden terminating...");
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
          Logger.Info($"{file.FullName}: verified");
          return true;
        }
        Logger.Info($"{file.FullName}: failed to verify");
      }
      return false;
    }

    private string ExtractFile(FileInfo file, string extractDir)
    {
      Logger.Info($"Extracting {file.FullName} to {extractDir}");

      try
      {
        ZipFile.ExtractToDirectory(file.FullName, extractDir);
      }
      catch (Exception ex)
      {
        Logger.Error(ex);
      }
      Logger.Info($"{file.FullName} extracted successfully.");
      return extractDir;
    }

    private void LoggerWithRobot(string strung)
    {
      Logger.Info(strung);

#if RELEASE
      synth.Speak(strung);
#endif

    }

    private (string, string, bool) SubstituteCommand(string command)
    {
      foreach (var item in CommandSuffixSubstitution)
      {
        var pattern = "^.+." + item.Item1 + "$";
        Regex regex = new Regex(pattern);
        var result = regex.Match(command);

        if (result.Success)
        {
          return (item.Item2, item.Item3, false);
        }
      }
      return (command, "", true);
    }

    private void ExecuteCommand(Manifest manifest, DirectoryInfo directory)
    {
      var proc = new Process();

      var soleCommand = SubstituteCommand(manifest.Command.Split(' ')[0]);

      // Remove original command from the string if it's not
      // filtered by command substitution. Else leave it in
      // as an argument to the subtituted command.
      string additionalArguments = (soleCommand.Item3)
        ? string.Join(" ", manifest.Command.Split(' ').Skip(1))
        : manifest.Command;

      proc.StartInfo.WorkingDirectory = (!string.IsNullOrEmpty(manifest.WorkingPath))
        ? Environment.ExpandEnvironmentVariables(manifest.WorkingPath)
        : directory.FullName;

      soleCommand.Item1 = (soleCommand.Item3)
        ? proc.StartInfo.WorkingDirectory + soleCommand.Item1
        : soleCommand.Item1;

      proc.StartInfo.Arguments = (!string.IsNullOrEmpty(manifest.Arguments))
        ? soleCommand.Item2 + " " + additionalArguments + " " + manifest.Arguments
        : soleCommand.Item2 + " " + additionalArguments;

      proc.StartInfo.Arguments =
        Environment.ExpandEnvironmentVariables(proc.StartInfo.Arguments);

      proc.StartInfo.FileName = soleCommand.Item1;
      proc.StartInfo.UseShellExecute = false;
      proc.StartInfo.RedirectStandardOutput = true;
      proc.StartInfo.RedirectStandardError = true;

      Logger.Info($"Starting {proc.StartInfo.FileName} {proc.StartInfo.Arguments}");
      proc.Start();
      proc.WaitForExit();

      var output = proc.StandardOutput.ReadToEnd();
      var errorOutput = proc.StandardError.ReadToEnd();
      if (!String.IsNullOrEmpty(output)) Logger.Info(output);
      if (!String.IsNullOrEmpty(errorOutput)) Logger.Info(output);

      if (proc.ExitCode != 0)
      {
        Logger.Info($"{manifest.Command} failed with exit code {proc.ExitCode}.");
        throw new Exception();
      }

      Logger.Info($"{manifest.Command} finished.");
    }

    private bool ManifestExists(DirectoryInfo directory)
    {
      var manifest = directory.GetFiles("manifest.yml")[0];
      return File.Exists(manifest.FullName);
    }

    private List<Manifest> OpenManifest(DirectoryInfo directory)
    {
      Logger.Info("Opening manifest...");

      var manifest = directory.GetFiles("manifest.yml")[0];
      using var manifestReader = new StreamReader(manifest.FullName);

      var deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

      var manifestEnumerator =
        YamlSerializerExtensions.DeserializeMany<Manifest>(deserializer, manifestReader);

      List<Manifest> manifestList = new List<Manifest>();

      foreach (var man in manifestEnumerator)
      {
        manifestList.Add(man);
      }

      return manifestList;
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

      if (HashLogText is null) HashLogText = File.ReadAllText(HashLog);

      if (HashLogText.Contains(hashCode + Environment.NewLine))
      {
        Logger.Info($"{hashCode} has already been processed.");

        return true;
      }

      return false;
    }

    private void CleanupExtractedFiles(FileInfo updateFile)
    {
      DirectoryInfo extractUpdateFolder =
        Directory.CreateDirectory(
          Path.Combine(Path.GetTempPath(),
          Path.GetFileNameWithoutExtension(updateFile.FullName)));

      extractUpdateFolder.Delete(true);
    }

    private DirectoryInfo GenerateReturnDirectory(FileInfo file)
    {
      return Directory.CreateDirectory(Path.Combine(file.DirectoryName,
        "log-" + Path.GetFileNameWithoutExtension(file.FullName)));
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
    private DirectoryInfo GetPayloadDirectory(FileInfo updateZipFile)
    {
      ExtractFile(updateZipFile, Path.GetTempPath());

      DirectoryInfo extractUpdateFolder =
        Directory.CreateDirectory(
          Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(updateZipFile.FullName)));

      if (extractUpdateFolder.Exists
        && FileVerification(extractUpdateFolder.GetFiles("payload.zip")[0]))
      {
        ExtractFile(extractUpdateFolder.GetFiles("payload.zip")[0], extractUpdateFolder.FullName);
        return Directory.CreateDirectory(Path.Combine(extractUpdateFolder.FullName, "payload"));
      }

      throw new Exception("Failed to grab payload folder.");
    }

    private void OnUsbDriveMounted(string path)
    {
      Logger.Info($"{path} was mounted.  Searching for Warden files...");
      DirectoryInfo dir = Directory.CreateDirectory(path);

      if (dir.Exists)
      {
        var files = dir.GetFiles("WARDEN*.zip");

        foreach (var updateFile in files)
        {
          FlushClientLog();

          if (UpdateAlreadyProcessed(StripHashCode(updateFile.Name)))
          {
            ReturnLogFile(updateFile);
            continue;
          }

          LoggerWithRobot("Beginning Warden Update");

          var payloadDir = GetPayloadDirectory(updateFile);

          if (ManifestExists(payloadDir))
          {
            var manifestList = OpenManifest(payloadDir);

            try
            {
              foreach (var manifest in manifestList)
              {
                try
                {
                  ExecuteCommand(manifest, payloadDir);
                }
                catch (Exception ex)
                {
                  if (manifest.OnError == "ignore")
                  {
                    Logger.Error(ex);
                    Logger.Info($"Ignoring error for {manifest.Command}");
                    UpdateErrors++;
                    continue;
                  }

                  throw ex;
                }
              }
            }
            catch (Exception ex)
            {
              Logger.Info($"Error processing manifest: {ex}");
              LoggerWithRobot("Warden update failed");
              ReturnLogFile(updateFile);
              CleanupExtractedFiles(updateFile);
              continue;
            }

            LoggerWithRobot($"Warden update completed with {UpdateErrors} error(s).");
            ReturnLogFile(updateFile);
            CleanupExtractedFiles(updateFile);
            LogHashCode(StripHashCode(updateFile.Name));
          }
        }
      }
      return;
    }

    private void OnUsbDriveEjected(string path)
    {
      Logger.Info($"{path} was ejected.");
    }
  }
}
