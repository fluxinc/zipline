using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using System.Threading;
using Mono.Options;
using System.Configuration.Install;

namespace DICOMCapacitorWarden
{
  internal static class Program
  {

    private const string ServiceName = "DicomCapacitorWarden";
    public static bool Quitting;
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main(string[] args)
    {

      var optionsSet = new OptionSet
      {
        { "i|install", "Install the service", n => InstallService(args) },
        { "u|uninstall", "Uninstall the service", _ => UninstallService() },
      };

      List<string> options = null;

      try
      {
        options = optionsSet.Parse(args);
      }
      catch (OptionException ex)
      {
        Console.WriteLine("DICOM Capacitor Warden Service");
        Console.WriteLine(ex.Message);
        return;
      }

      if (Quitting) return;

      // Ensure the cwd is set properly.
      FileInfo file = new FileInfo(Assembly.GetExecutingAssembly().Location);
      string cwd = file.DirectoryName;
      Directory.SetCurrentDirectory(cwd);

      var service = new WindowsService();
      var servicesToRun = new ServiceBase[] { service };
      ServiceBase.Run(servicesToRun);
    }

    private static void InstallService(string[] args)
    {
      var serviceController = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == ServiceName);

      if (serviceController != null)
        ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location });

      var projectInstaller = new ProjectInstaller();

      var serviceArgs = args.Where(a => a != "--install").Select(a => $"\"{a}\"");
      var cmdline = string.Join(" ", serviceArgs.Prepend($"/assemblypath={Assembly.GetExecutingAssembly().Location}"));
      projectInstaller.Context = new InstallContext(null, new string[] { cmdline });

      var state = new System.Collections.Specialized.ListDictionary();
      projectInstaller.Install(state);

      //      ManagedInstallerClass.InstallHelper(new[] { assemblyLocation });
      Quitting = true;
    }

    private static void UninstallService()
    {
      var serviceController = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == ServiceName);
      var assemblyLocation = Assembly.GetExecutingAssembly().Location;
      if (serviceController != null)
        ManagedInstallerClass.InstallHelper(new[] { "/u", assemblyLocation });
      else
        Console.WriteLine($@"{ServiceName} is not installed.");
      Quitting = true;
    }
  }
}
