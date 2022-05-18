using System;
using System.IO;
using System.Reflection;

namespace DICOMCapacitorWarden.util
{
  public static class Globals
  {
    private static string _logDirPath;
    public static string ManualAssemblyProduct = null;
    private static string _commonAppFolder;
    private static Assembly _Assembly;

    public static Assembly AppAssembly
    {
      set => _Assembly = value;
      get
      {
        if (null == _Assembly)
          return Assembly.GetExecutingAssembly();
        return _Assembly;
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
    public static string CommonAppFolderSubdirectory(string name)
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

    public static string CommonAppFolder
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

    public static string AssemblyProduct
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
