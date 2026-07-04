using PitStopVR.Configuration.Backup;
using PitStopVR.Core.Models;
using PitStopVR.Knowledge.Models;
using System.Diagnostics;

namespace PitStopVR.Configuration.Appliers;

public sealed class OdtApplier : IConfigurationApplier
{
    private readonly BackupManager _backupManager;

    public string Name => "Oculus Debug Tool";

    public OdtApplier(BackupManager backupManager)
    {
        _backupManager = backupManager;
    }

    public bool CanApply(MachineProfile profile)
    {
        return profile.Software.MetaQuestLinkInstalled || FindAdb() is not null;
    }

    public Task<ApplyResult> ApplyAsync(MachineProfile profile, Profile profileSettings)
    {
        var result = new ApplyResult { ComponentName = Name };

        try
        {
            var adbPath = FindAdb();
            if (string.IsNullOrWhiteSpace(adbPath))
            {
                result.Success = false;
                result.ErrorMessage = "No se encontro adb.exe. Conecta el Quest con USB debugging habilitado o instala Android Platform Tools.";
                return Task.FromResult(result);
            }

            var commands = BuildAdbCommands(profileSettings.Odt);
            var executed = new List<string>();

            foreach (var command in commands)
            {
                var output = ExecuteAdb(adbPath, command);
                executed.Add($"adb {command} -> {output}");
            }

            result.Success = true;
            result.Message = string.Join("\n", executed);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return Task.FromResult(result);
    }

    private static string? FindAdb()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk", "platform-tools", "adb.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Oculus", "Support", "oculus-diagnostics", "adb.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Oculus", "Support", "oculus-diagnostics", "adb.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local", "Android", "Sdk", "platform-tools", "adb.exe"),
            @"C:\Program Files (x86)\Minimal ADB and Fastboot\adb.exe",
            @"C:\adb\adb.exe"
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var path in pathVariable.Split(Path.PathSeparator))
        {
            var fullPath = Path.Combine(path, "adb.exe");
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }

    private static IEnumerable<string> BuildAdbCommands(OdtSettings settings)
    {
        // Bitrate de video en Mbps (multiplicado por 1.000.000 para obtener bits/segundo)
        yield return $"shell setprop debug.oculus.capture.bitrate {settings.BitrateMbps * 1000000}";

        // Adaptive GPU scale
        yield return $"shell setprop debug.oculus.adaptiveGpuScale {(settings.AdaptiveGpuScale ? "1" : "0")}";

        // FOV tangente (0 = maximo FOV)
        yield return $"shell setprop debug.oculus.foveation.tangent {settings.FovTangent}";

        // Foveation level (0=off, 1=low, 2=medium, 3=high)
        yield return $"shell setprop debug.oculus.foveation.level {settings.FoveationLevel}";

        // Refresh rate del display del Quest
        yield return $"shell setprop debug.oculus.refreshRate {settings.RefreshRate}";
    }

    private static string ExecuteAdb(string adbPath, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = adbPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)!;
        process.WaitForExit();

        var output = process.StandardOutput.ReadToEnd().Trim();
        var error = process.StandardError.ReadToEnd().Trim();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"ADB fallo con '{arguments}': {error}");
        }

        return string.IsNullOrWhiteSpace(output) ? "OK" : output;
    }
}
