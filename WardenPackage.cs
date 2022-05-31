using DICOMCapacitorWarden.util;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static DICOMCapacitorWarden.util.VerifyDetachedSignature;

namespace DICOMCapacitorWarden
{
  class WardenPackage
  {
    private static readonly ILog Logger = LogManager.GetLogger("WardenLog");
    private static readonly string HashLog = Path.Combine(Globals.LogDirPath, "hash.log");
    private static readonly string ClientLog = Path.Combine(Globals.LogDirPath, "update.log");
    public static string HashLogText = null;
    public static int UpdateErrors = 0;

    private static List<(string, string, string)> CommandSuffixSubstitution =
      new List<(string, string, string)>
      {
        // extension | substitution | arguments-to-substitution
        ("bat", "cmd.exe", "/c" ),
        ("ps1", "powershell.exe", "-File")
      };

#if RELEASE
    private SpeechSynthesizer synth => new SpeechSynthesizer();
#endif
    private static bool FileVerification(FileInfo file)
    {
      if (!File.Exists($"{file.FullName}.sig")) return false;

      FileInfo fileInfo = new FileInfo(file.FullName);
      FileInfo signature = new FileInfo($"{file.FullName}.sig");
      Logger.Info($"Verifying {file.FullName} against signature {signature.FullName}");

      // In this method we can just sign .zips using pgp.
      // If the .zip is modified the signature should fail
      // giving us checksums for free too. 

      if (!VerifySignature(fileInfo.FullName, signature.FullName)) return false;

      return true;
    }

    private static string ExtractFile(FileInfo file, string extractDir)
    {
      Logger.Info($"Extracting {file.FullName} to {extractDir}");

      try
      {
        ZipFile.ExtractToDirectory(file.FullName, extractDir);
      }
      catch (Exception ex)
      {
        throw new Exception($"Failed to extract {file.FullName}", ex);
      }

      Logger.Info($"{file.FullName} extracted successfully.");
      return extractDir;
    }

    private static void LoggerWithRobot(string strung)
    {
      Logger.Info(strung);

#if RELEASE
      synth.Speak(strung);
#endif
    }

    private static (string, string, bool) SubstituteCommand(string command)
    {
      foreach (var item in CommandSuffixSubstitution)
      {
        var pattern = "^.+." + item.Item1 + "$";
        Regex regex = new Regex(pattern);
        var result = regex.Match(command);

        if (result.Success) return (item.Item2, item.Item3, false);
      }
      return (command, "", true);
    }

    private static void ExecuteCommand(Manifest manifest, DirectoryInfo directory)
    {
      var proc = new Process();

      var soleCommand = SubstituteCommand(manifest.Command.Split(' ')[0]);

      // Remove original command from the string if it's not
      // filtered by command substitution. Else leave it in
      // as an argument to the subtituted command.
      string additionalArguments = (soleCommand.Item3)
        ? string.Join(" ", manifest.Command.Split(' ').Skip(1))
        : manifest.Command;

      proc.StartInfo.WorkingDirectory = (!string.IsNullOrEmpty(manifest.WorkingPath))
        ? Environment.ExpandEnvironmentVariables(manifest.WorkingPath)
        : directory.FullName;

      soleCommand.Item1 = (soleCommand.Item3)
        ? proc.StartInfo.WorkingDirectory + soleCommand.Item1
        : soleCommand.Item1;

      proc.StartInfo.Arguments = (!string.IsNullOrEmpty(manifest.Arguments))
        ? soleCommand.Item2 + " " + additionalArguments + " " + manifest.Arguments
        : soleCommand.Item2 + " " + additionalArguments;

      proc.StartInfo.Arguments =
        Environment.ExpandEnvironmentVariables(proc.StartInfo.Arguments);

      proc.StartInfo.FileName = soleCommand.Item1;
      proc.StartInfo.UseShellExecute = false;
      proc.StartInfo.RedirectStandardOutput = true;
      proc.StartInfo.RedirectStandardError = true;

      Logger.Info($"Starting {proc.StartInfo.FileName} {proc.StartInfo.Arguments}");
      proc.Start();
      proc.WaitForExit();

      var output = proc.StandardOutput.ReadToEnd();
      var errorOutput = proc.StandardError.ReadToEnd();
      if (!String.IsNullOrEmpty(output)) Logger.Info(output);
      if (!String.IsNullOrEmpty(errorOutput)) Logger.Info(output);

      if (proc.ExitCode != 0) throw new Exception($"{manifest.Command} failed with exit code {proc.ExitCode}.");

      Logger.Info($"{manifest.Command} finished.");
    }

    private static List<Manifest> OpenManifest(DirectoryInfo directory)
    {
      Logger.Info("Opening manifest...");

      try
      {
        var manifestFile = directory.GetFiles("manifest.yml")[0];

        using var manifestReader = new StreamReader(manifestFile.FullName);

        var deserializer = new DeserializerBuilder()
          .WithNamingConvention(CamelCaseNamingConvention.Instance)
          .Build();

        var manifestEnumerator =
          YamlSerializerExtensions.DeserializeMany<Manifest>(deserializer, manifestReader);

        List<Manifest> manifests = new List<Manifest>();

        foreach (var manifest in manifestEnumerator) manifests.Add(manifest);

        return manifests;
      }
      catch (Exception ex)
      {
        throw new Exception("Invalid or missing manifest.", ex);
      }
    }

    private static string StripHashCode(string filename)
    {
      var match = Regex.Match(filename, "^WARDEN-(.*).zip");
      return match.Groups[1].Value;
    }

    private static void LogHashCode(string hashcode)
    {
      File.AppendAllText(HashLog, hashcode + Environment.NewLine);
      HashLogText = null;
    }

    private static bool UpdateAlreadyProcessed(string hashCode)
    {
      if (!File.Exists(HashLog)) File.Create(HashLog);

      if (HashLogText is null) HashLogText = File.ReadAllText(HashLog);

      return HashLogText.Contains(hashCode + Environment.NewLine);
    }

    private static void CleanupExtractedFiles(FileInfo updateFile)
    {
      DirectoryInfo extractUpdateFolder =
        Directory.CreateDirectory(
          Path.Combine(Path.GetTempPath(),
          Path.GetFileNameWithoutExtension(updateFile.FullName)));

      extractUpdateFolder.Delete(true);
    }

    private static DirectoryInfo GenerateReturnDirectory(FileInfo file)
    {
      return Directory.CreateDirectory(Path.Combine(file.DirectoryName,
        "log-" + Path.GetFileNameWithoutExtension(file.FullName)));
    }

    private static void ReturnLogFile(FileInfo file)
    {
      FileInfo log = new FileInfo(ClientLog);

      DirectoryInfo returnDir = GenerateReturnDirectory(file);

      log.CopyTo(returnDir.FullName + $"\\{log.Name}", true);

      if (File.Exists(returnDir.FullName + ".zip"))
        File.Delete(returnDir.FullName + ".zip");

      ZipFile.CreateFromDirectory(returnDir.FullName, returnDir.FullName + ".zip");

      returnDir.Delete(true);
    }

    private static void FlushClientLog()
    {
      FileInfo log = new FileInfo(ClientLog);
      File.Delete(log.FullName);
      File.WriteAllText(log.FullName, "");
    }

    private static DirectoryInfo GetPayloadDirectory(FileInfo updateZipFile)
    {
      ExtractFile(updateZipFile, Path.GetTempPath());

      DirectoryInfo extractUpdateFolder =
        Directory.CreateDirectory(
          Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(updateZipFile.FullName)));

      var payloadZipFile = extractUpdateFolder.GetFiles("payload.zip")[0];

      if (!FileVerification(payloadZipFile)) throw new Exception("Bad payload signature or missing payload.");

      ExtractFile(payloadZipFile, extractUpdateFolder.FullName);
      return Directory.CreateDirectory(Path.Combine(extractUpdateFolder.FullName, "payload"));
    }

    private static void ProcessManifest(FileInfo updateZipFile)
    {
      var payloadDir = GetPayloadDirectory(updateZipFile);
      var manifests = OpenManifest(payloadDir);

      foreach (var manifest in manifests)
      {
        try
        {
          switch (manifest.Type.ToLower())
          {
            case "run":
              ExecuteCommand(manifest, payloadDir);
              break;
            default:
              Logger.Info($"Type {manifest.Type} unimplemented.");
              break;
          }
        }
        catch (Exception ex)
        {
          if (manifest.OnError == "abort") throw ex;

          Logger.Error(ex);
          Logger.Info($"Ignoring error for {manifest.Command}");
          UpdateErrors++;
          continue;
        }
      }

      LoggerWithRobot($"Warden update completed with {UpdateErrors} error(s).");
      ReturnLogFile(updateZipFile);
      CleanupExtractedFiles(updateZipFile);
      LogHashCode(StripHashCode(updateZipFile.Name));
    }

    public static void ProcessUpdate(FileInfo updateZipFile)
    {
      FlushClientLog();
      UpdateErrors = 0;

      if (UpdateAlreadyProcessed(StripHashCode(updateZipFile.Name)))
      {
        Logger.Info($"{updateZipFile} has already been processed.");
        ReturnLogFile(updateZipFile);
        return;
      }

      LoggerWithRobot("Beginning Warden Update");

      try
      {
        ProcessManifest(updateZipFile);
      }
      catch (Exception ex)
      {
        Logger.Info($"Error processing manifest: {ex}");
        LoggerWithRobot("Warden update failed");
        ReturnLogFile(updateZipFile);
        CleanupExtractedFiles(updateZipFile);
        return;
      }
    }
  }
}
