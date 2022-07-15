using Zipline.Utility;
using log4net;
using log4net.Config;
using log4net.Core;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;

namespace Zipline
{
  internal static class Program
  {
    private const string ServiceName = "Zipline";
    private static bool Quitting;

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
        Console.WriteLine("Zipline Service");
        Console.WriteLine(ex.Message);
        return;
      }

      if (Quitting) return;

      SetupLog4Net();

      // Ensure the cwd is set properly.
      var file = new FileInfo(Assembly.GetExecutingAssembly().Location);
      var cwd = file.DirectoryName;
      Directory.SetCurrentDirectory(cwd ?? string.Empty);

      var service = new WindowsService();

      if (!Environment.UserInteractive)
      {
        var servicesToRun = new ServiceBase[] { service };
        Process.Start("sc", $"failure {ServiceName} reset= 86400 actions=restart/120000/restart/120000//");
        ServiceBase.Run(servicesToRun);
      }
      else
      {
        service.TestStartupAndStop(args);
      }
    }

    private static void SetupLog4Net()
    {
      GlobalContext.Properties["LogName"] = Path.Combine(Globals.LogDirPath, "zipline.log");
      GlobalContext.Properties["UpdateLogName"] = Path.Combine(Globals.LogDirPath, "update.log");
      XmlConfigurator.Configure();
#if DEBUG || DEBUG_ACTIVATION
      ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root.Level = Level.All;
#else
      ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root.Level = Level.Info;
#endif
      ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);
    }

    private static void InstallService(IEnumerable<string> args)
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
