using System;
using System.IO;
using System.ServiceProcess;
using Usb.Events;
using static Warden.util.VerifyDetachedSignature;
using log4net;
using System.IO.Compression;


namespace DICOMCapacitorWarden
{
  public partial class WindowsService : ServiceBase
  {
    private static readonly ILog Logger = LogManager.GetLogger(typeof(WindowsService));
    private bool QUITTING => false;

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

    private void ProcessFile(FileInfo file)
    {
      Logger.Info($"Extracting {file} to {file.DirectoryName}");

      try
      {
        ZipFile.ExtractToDirectory(file.FullName, file.DirectoryName);
      }
      catch (Exception ex)
      {
        Logger.Error(ex);
        return;
      }

      Logger.Info($"{file} extracted successfully.");
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
          if (FileVerification(file))
          {
            ProcessFile(file);
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
