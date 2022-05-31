using System;
using System.IO;
using System.Reflection;

namespace DICOMCapacitorWarden.util
{
  public static class Globals
  {
    private static string _logDirPath;
    private static readonly string ManualAssemblyProduct = null;
    private static string _commonAppFolder;
    private static Assembly _assembly;

    private static Assembly AppAssembly
    {
      set => _assembly = value;
      get
      {
        if (null == _assembly)
          return Assembly.GetExecutingAssembly();
        return _assembly;
      }
    }

    public static string LogDirPath
    {
      get
      {
        if (string.IsNullOrEmpty(_logDirPath))
          _logDirPath = CommonAppFolderSubdirectory("log");
        return _logDirPath;
      }
      set => _logDirPath = value;
    }

    private static string CommonAppFolderSubdirectory(string name)
    {
      var tmp = Path.Combine(CommonAppFolder, name);

      try
      {
        if (!Directory.Exists(tmp))
          Directory.CreateDirectory(tmp);

        return tmp;
      }
      catch (Exception e)
      {
        Console.WriteLine("WARNING: CommonAppFolderSubdirectory('{0}') doesn't seem to exist: '{1}'", tmp,
          e.Message);
      }

      return null;
    }

    private static string CommonAppFolder
    {
      get
      {
        if (!string.IsNullOrEmpty(_commonAppFolder)) return _commonAppFolder;
        var tmp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
          "Flux Inc", AssemblyProduct);
        try
        {
          if (!Directory.Exists(tmp))
            Directory.CreateDirectory(tmp);

          _commonAppFolder = tmp;
        }
        catch (Exception e)
        {
          _commonAppFolder = null;
          Console.WriteLine("WARNING: CommonAppFolder doesn't seem to exist: '{0}'", e.Message);
        }

        return _commonAppFolder;
      }
      set => _commonAppFolder = value;
    }

    private static string AssemblyProduct
    {
      get
      {
        if (ManualAssemblyProduct != null)
          return ManualAssemblyProduct;

        var attributes = AppAssembly
          .GetCustomAttributes(typeof(AssemblyProductAttribute), false);
        if (attributes.Length == 0)
          return "";
        return ((AssemblyProductAttribute)attributes[0]).Product;
      }
    }
  }
}
