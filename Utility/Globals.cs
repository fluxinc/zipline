using log4net;
using System;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Reflection;
using YamlDotNet.Serialization;

namespace Zipline.Utility
{
  public static class Globals
  {
    private static string _logDirPath;
    private static readonly string ManualAssemblyProduct = null;
    private static string _commonAppFolder;
    private static Assembly _assembly;
    public static string ConfigFilePath => Path.Combine(CommonAppFolder, "config.yml");
    public static bool GlobalConfigurationExists => File.Exists(ConfigFilePath);
    private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);


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

    public static bool LoadGlobalDefaults<T>(T settings, bool resetFirst = true) where T : ApplicationSettingsBase
    {
      try
      {
        if (resetFirst) settings.Reset();

        if (GlobalConfigurationExists)
        {
          Logger.Info($"Loading configuration from '{ConfigFilePath}'.");

          var deserializer = new Deserializer();
          using (TextReader reader = File.OpenText(ConfigFilePath))
          {
            var defaults = deserializer.Deserialize<Hashtable>(reader);
            foreach (SettingsProperty property in settings.Properties)
              if (defaults[property.Name] != null)
              {
                Logger.Info($"  {property.Name} = '{defaults[property.Name]}'");

                var propType = settings[property.Name].GetType();

                if (propType == typeof(bool))
                {
                  settings[property.Name] = Convert.ToBoolean((string)defaults[property.Name]);
                }
                else
                {
                  settings[property.Name] = (string)defaults[property.Name];
                }
              }
          }

          settings.Save();
          Logger.Info("Loaded configuration.");

          foreach (SettingsPropertyValue value in settings.PropertyValues)
          {
            Logger.Info($"  {value.Name} = '{value.PropertyValue}'");
          }

        }
        else
        {
          Logger.Info("No configuration file to load, skipping.");
        }

        return true;
      }
      catch (Exception ex)
      {
        Logger.Warn($"Exception while loading configuration from '{ConfigFilePath}':", ex);
        throw;
      }
    }
  }
}
