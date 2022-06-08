using log4net;
using System.IO;
using System.ServiceProcess;
using System.Speech.Synthesis;
using Usb.Events;

namespace DICOMCapacitorWarden
{
  public partial class WindowsService : ServiceBase
  {
    private static readonly ILog Logger = LogManager.GetLogger("WardenLog");
    private static bool Quitting => false;
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
      while (!Quitting) { }
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

    private void OnUsbDriveMounted(string path)
    {
      Logger.Info($"{path} was mounted.  Searching for Warden files...");
      var dir = Directory.CreateDirectory(path);

      if (!dir.Exists) return;

      var files = dir.GetFiles("warden*.zip");
      if (files.Length > 0) { Finished = true; }

      foreach (var updateZipFile in files)
      {
        var wardenPackage = new WardenPackage(updateZipFile);
        wardenPackage.Update();
      }

      if (Finished) LoggerWithRobot("All updates complete. You may now remove the drive.");
      Finished = false;
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
