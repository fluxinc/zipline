using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DICOMCapacitorWarden.util
{
  class FileCopy
  {

    private static readonly ILog Logger = LogManager.GetLogger(typeof(WindowsService));

    public static bool CopyFolderContents(string SourcePath, string DestinationPath)
    {
      SourcePath = SourcePath.EndsWith(@"\") ? SourcePath : SourcePath + @"\";
      DestinationPath = DestinationPath.EndsWith(@"\") ? DestinationPath : DestinationPath + @"\";

      try
      {
        if (Directory.Exists(SourcePath))
        {
          if (Directory.Exists(DestinationPath) == false)
          {
            Directory.CreateDirectory(DestinationPath);
            Logger.Info($"Creating Directory: {DestinationPath}");
          }

          foreach (string files in Directory.GetFiles(SourcePath))
          {
            FileInfo fileInfo = new FileInfo(files);
            fileInfo.CopyTo(string.Format(@"{0}\{1}", DestinationPath, fileInfo.Name), true);
            Logger.Info($"Copying File: {fileInfo.FullName} to {DestinationPath}");
          }

          foreach (string drs in Directory.GetDirectories(SourcePath))
          {
            DirectoryInfo directoryInfo = new DirectoryInfo(drs);
            if (CopyFolderContents(drs, DestinationPath + directoryInfo.Name) == false)
            {
              return false;
            }
          }
        }
        return true;
      }
      catch (Exception ex)
      {
        return false;
      }
    }
  }
}
