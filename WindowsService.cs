using System;
using System.IO;
using System.ServiceProcess;
using Usb.Events;
using static Warden.util.VerifyDetachedSignature;
using log4net;
using System.IO.Compression;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using DICOMCapacitorWarden.util;
using System.Diagnostics;
using System.Speech.Synthesis;

namespace DICOMCapacitorWarden
{
  public partial class WindowsService : ServiceBase
  {
    private static readonly ILog Logger = LogManager.GetLogger(typeof(WindowsService));
    private bool QUITTING => false;
    private SpeechSynthesizer synth => new SpeechSynthesizer();

    public WindowsService()
    {
      InitializeComponent();
    }

    internal void TestStartupAndStop(string[] args)
    {
      OnStart(args);
      OnStop();
    }

    protected override void OnStart(string[] args)
    {
      var usbEventWatcher = new UsbEventWatcher();

      usbEventWatcher.UsbDriveEjected += (_, path) => OnUsbDriveEjected(path);
      usbEventWatcher.UsbDriveMounted += (_, path) => OnUsbDriveMounted(path);

      while (!QUITTING) { };
    }

    protected override void OnStop()
    {

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

    private void ExecutableOperation(Manifest manifest, DirectoryInfo directory)
    {
      Logger.Info($"Starting {manifest.Executable}");
      ProcessStartInfo processStartInfo = new ProcessStartInfo();
      processStartInfo.WorkingDirectory = directory.FullName;
      processStartInfo.FileName = Path.Combine(directory.FullName, manifest.Executable);
      Process.Start(processStartInfo);
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

    private void ProcessManifest(DirectoryInfo directory)
    {
      var manifest = directory.GetFiles("manifest.yml")[0];

      if (!File.Exists(manifest.FullName))
      {
        Logger.Info($"No valid manifest file found. {manifest.FullName}");
        return;
      }

      var manifestText = File.ReadAllText(manifest.FullName);
      var deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();
      var manifestData = deserializer.Deserialize<Manifest>(manifestText);

      switch (manifestData.Operation.ToLower())
      {
        case "executable":
          Logger.Info("Running Executable Operation from Manifest...");
          ExecutableOperation(manifestData, directory);
          break;

        case "filecopy":
          Logger.Info("Running File Copy from Manifest...");
          FileCopyOperation(manifestData, directory);
          break;

        default:
          Logger.Info("Manifest doesn't contain a valid operation.");
          break;
      }
    }

    private void OnUsbDriveMounted(string path)
    {
      Logger.Info($"{path} was mounted.  Searching for Warden Files...");
      DirectoryInfo dir = Directory.CreateDirectory(path);

      if (dir.Exists)
      {
        var files = dir.GetFiles("WARDEN*.zip");

        foreach(var file in files) 
        {
          ExtractFile(file);
          DirectoryInfo extractDir =
            Directory.CreateDirectory(Path.Combine(file.DirectoryName, Path.GetFileNameWithoutExtension(file.FullName)));

          if (extractDir.Exists && FileVerification(extractDir.GetFiles("payload.zip")[0]))
          {

            Logger.Info($"Processing payload in {extractDir.FullName}");
            ExtractFile(extractDir.GetFiles("payload.zip")[0]);

            ProcessManifest(Directory.CreateDirectory(Path.Combine(extractDir.FullName, "payload")));
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
