using log4net;
using System;
using System.IO;
using System.ServiceProcess;
using Usb.Events;

namespace Zipline
{
  public partial class WindowsService : ServiceBase
  {
    private static readonly ILog Logger = LogManager.GetLogger("ZiplineLog");

    private static bool Finished;

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
      Console.WriteLine("\nPress any key to terminate Zipline...");
      Console.ReadLine();
      Console.WriteLine("\nZipline terminating...\n");
      OnStop();
    }

    protected override void OnStart(string[] args)
    {
      Logger.Info("Starting Zipline...");
      var usbEventWatcher = new UsbEventWatcher();

      usbEventWatcher.UsbDriveEjected += (_, path) => OnUsbDriveEjected(path);
      usbEventWatcher.UsbDriveMounted += (_, path) => OnUsbDriveMounted(path);
    }

    protected override void OnStop()
    {
    }

    private void OnUsbDriveMounted(string path)
    {
      Logger.Info($"{path} was mounted.  Searching for Zipline files...");
      var dir = Directory.CreateDirectory(path);

      if (!dir.Exists) return;

      var files = dir.GetFiles("zipline*.zip");

      if (files.Length == 0) { return; }

      foreach (var updateZipFile in files)
      {
        var ziplinePackage = new ZiplinePackage(updateZipFile);
        ziplinePackage.Update();
      }

      LoggerWithRobot("All updates complete. You may now remove the flash drive.");
    }

    private static void OnUsbDriveEjected(string path)
    {
      Logger.Info($"{path} was ejected.");
    }

    private void LoggerWithRobot(string strung)
    {
      Logger.Info(strung);

#if RELEASE
      synth.Speak(strung);
#endif
    }
  }
}
