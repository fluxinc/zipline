using log4net;
using System.IO;
using System.ServiceProcess;
using Usb.Events;

namespace DICOMCapacitorWarden
{
  public partial class WindowsService : ServiceBase
  {
    private static readonly ILog Logger = LogManager.GetLogger("WardenLog");
    private bool QUITTING => false;

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

    private void OnUsbDriveMounted(string path)
    {
      Logger.Info($"{path} was mounted.  Searching for Warden files...");
      DirectoryInfo dir = Directory.CreateDirectory(path);

      if (!dir.Exists) return;

      var files = dir.GetFiles("WARDEN*.zip");

      foreach (var updateZipFile in files)
      {
        WardenPackage.ProcessUpdate(updateZipFile);
      }
    }

    private void OnUsbDriveEjected(string path)
    {
      Logger.Info($"{path} was ejected.");
    }
  }
}
