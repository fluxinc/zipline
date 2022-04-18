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

namespace DICOMCapacitorWarden
{
  internal static class Program
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main(string[] args)
    {
      var optionsSet = new OptionSet
      {

      };
      var service = new WindowsService();
      service.TestStartupAndStop(args);
    }
  }
}
