﻿using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Zipline.Utility;
using static Zipline.Utility.VerifyDetachedSignature;
using System.Speech.Synthesis;

namespace Zipline
{
  public class ZiplinePackage
  {
    private static readonly ILog Logger = LogManager.GetLogger("ZiplineLog");
    private static readonly string HashLog = Path.Combine(Globals.LogDirPath, "hash.log");
    private static readonly string ClientLog = Path.Combine(Globals.LogDirPath, "update.log");

    private static readonly string AdditionalReturnDirectory =
      Environment.ExpandEnvironmentVariables("%tmp%\\ZiplineReturnDirectory\\");

    public bool IgnoreHashLog { get; set; }
    private string TempPath => Path.GetTempPath();
    private List<Manifest> Manifests { get; set; }
    private FileInfo UpdateZipFile { get; set; }
    private DirectoryInfo PayloadDirectory { get; set; }
    private DirectoryInfo ExtractUpdateFolder { get; set; }

    public static string HashLogText = null;
    public int UpdateErrors = 0;

    /* Any executable/script in the manifest.yml will be filtered through this
     * list.  You can add substitutions and flags to that substitution if you'd
     * like to modify how a particular executable/script will be run.
    */
    private static List<(string, string, string)> CommandSuffixSubstitution =
      new List<(string, string, string)>
      {
        // extension | substitution | arguments-to-substitution
        ("bat", "cmd.exe", "/c .\\" ),
        ("ps1", "cmd.exe", "/c powershell.exe -NoProfile -NoLogo -NonInteractive -ExecutionPolicy Bypass -Command .\\")
      };

#if RELEASE
    private SpeechSynthesizer synth => new SpeechSynthesizer();
#endif

    public ZiplinePackage(FileInfo updatePath)
    {
      UpdateZipFile = updatePath;

      ExtractUpdateFolder = Directory.CreateDirectory(
        Path.Combine(TempPath, Path.GetFileNameWithoutExtension(UpdateZipFile.FullName)));

      PayloadDirectory = Directory.CreateDirectory(
        Path.Combine(ExtractUpdateFolder.FullName, "payload"));

      Manifests = new List<Manifest>();
    }

    private static void ExtractFile(FileInfo file, string extractDirectory)
    {
      Logger.Info($"Extracting {file.FullName} to {extractDirectory}");

      try
      {
        var extractUpdateFolder = Path.Combine(
          extractDirectory, Path.GetFileNameWithoutExtension(file.FullName));

        if (Directory.Exists(extractUpdateFolder))
          Directory.Delete(extractUpdateFolder, true);
        ZipFile.ExtractToDirectory(file.FullName, extractDirectory);
      }
      catch (Exception ex)
      {
        throw new Exception($"Failed to extract {file.FullName}", ex);
      }

      Logger.Info($"{file.FullName} extracted successfully.");
    }

    private void LoggerWithRobot(string strung)
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
        ? soleCommand.Item2 + additionalArguments + " " + manifest.Arguments
        : soleCommand.Item2 + additionalArguments;

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

    private void OpenManifest()
    {
      Logger.Info("Opening manifest...");

      try
      {
        var manifestFile = PayloadDirectory.GetFiles("manifest.yml")[0];

        using var manifestReader = new StreamReader(manifestFile.FullName);

        var deserializer = new DeserializerBuilder()
          .WithNamingConvention(CamelCaseNamingConvention.Instance)
          .Build();

        var manifestEnumerator =
          YamlSerializerExtensions.DeserializeMany<Manifest>(deserializer, manifestReader);

        foreach (var manifest in manifestEnumerator) Manifests.Add(manifest);

      }
      catch (Exception ex)
      {
        throw new Exception("Invalid or missing manifest.", ex);
      }
    }

    private static string StripHashCode(string filename)
    {
      var match = Regex.Match(filename, "^zipline-(.*).zip");
      return match.Groups[1].Value;
    }

    private static void LogHashCode(string hashCode)
    {
      var match = Regex.Match(hashCode, "^repeat_(.*)");
      if (match.Success) return;

      File.AppendAllText(HashLog, hashCode + Environment.NewLine);
      HashLogText = null;
    }

    private static bool UpdateAlreadyProcessed(string hashCode)
    {
      if (!File.Exists(HashLog)) File.Create(HashLog);

      if (HashLogText is null) HashLogText = File.ReadAllText(HashLog);

      return HashLogText.Contains(hashCode + Environment.NewLine);
    }

    private void CleanupExtractedFiles(FileInfo updateFile)
    {
      ExtractUpdateFolder.Delete(true);
    }

    private static DirectoryInfo GenerateReturnDirectory(FileInfo file)
    {
      return Directory.CreateDirectory(Path.Combine(file.DirectoryName,
        "log-" + Path.GetFileNameWithoutExtension(file.FullName)));
    }

    private static void CopyAdditionalReturnFiles(
      DirectoryInfo returndir, DirectoryInfo additionReturnDir)
    {
      foreach (DirectoryInfo dir in additionReturnDir.GetDirectories())
        CopyAdditionalReturnFiles(returndir.CreateSubdirectory(dir.Name), dir);

      foreach (FileInfo file in additionReturnDir.GetFiles())
        file.CopyTo(Path.Combine(returndir.FullName, file.Name));
    }

    private static void ReturnLogFile(FileInfo file)
    {
      FileInfo log = new FileInfo(ClientLog);

      DirectoryInfo returnDir = GenerateReturnDirectory(file);
      DirectoryInfo additionReturnDir = new DirectoryInfo(AdditionalReturnDirectory);

      log.CopyTo(returnDir.FullName + $"\\{log.Name}", true);

      CopyAdditionalReturnFiles(returnDir, additionReturnDir);

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

    private void ExtractPayload()
    {
      ExtractFile(UpdateZipFile, TempPath);
      var payloadZipFile = ExtractUpdateFolder.GetFiles("payload.zip")[0];

      if (!VerifyFile(payloadZipFile)) throw new Exception("Bad payload signature or missing payload.");

      ExtractFile(payloadZipFile, ExtractUpdateFolder.FullName);
    }

    private void ProcessManifest()
    {
      foreach (var manifest in Manifests)
      {
        try
        {
          switch (manifest.Type.ToLower())
          {
            case "run":
              ExecuteCommand(manifest, PayloadDirectory);
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
      FinalizeUpdate(true);
    }

    private void FinalizeUpdate(bool status)
    {
      var logMessage = (status) ? "completed" : "failed";

      LoggerWithRobot($"Zipline update {logMessage} with {UpdateErrors} errors.");
      ReturnLogFile(UpdateZipFile);
      CleanupExtractedFiles(UpdateZipFile);

      if (!IgnoreHashLog && status) LogHashCode(StripHashCode(UpdateZipFile.Name));
      return;
    }

    public bool PrepareUpdate()
    {
      FlushClientLog();
      SetupReturnFolder();

      if (!IgnoreHashLog && UpdateAlreadyProcessed(StripHashCode(UpdateZipFile.Name)))
      {
        Logger.Info($"{UpdateZipFile.Name} has already been processed.");
        ReturnLogFile(UpdateZipFile);
        return false;
      }
      try
      {
        ExtractPayload();
      }
      catch (Exception ex)
      {
        Logger.Error(ex);
        UpdateErrors++;
        FinalizeUpdate(false);
        return false;
      }

      return true;
    }

    private void SetupReturnFolder()
    {
      try
      {
        Directory.Delete(AdditionalReturnDirectory, true);
      }
      catch (DirectoryNotFoundException ex)
      {

      }
      Directory.CreateDirectory(AdditionalReturnDirectory);
    }

    public bool ProcessUpdate()
    {
      LoggerWithRobot("Beginning Zipline Update");
      try
      {
        OpenManifest();
        ProcessManifest();
      }
      catch (Exception ex)
      {
        Logger.Error($"Error processing manifest: {ex}");
        UpdateErrors++;
        FinalizeUpdate(false);
        return false;
      }
      return true;
    }

    public void Update()
    {
      if (PrepareUpdate()) ProcessUpdate();
    }
  }
}
