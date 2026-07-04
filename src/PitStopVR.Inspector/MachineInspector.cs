using Microsoft.Win32;
using PitStopVR.Core.Models;
using System.Management;
using System.Text.RegularExpressions;

namespace PitStopVR.Inspector;

public sealed class MachineInspector
{
    public MachineProfile Inspect()
    {
        var profile = new MachineProfile();
        InspectHardware(profile.Hardware);
        InspectSoftware(profile.Software);
        profile.Games = InspectSteamGames().ToList();
        return profile;
    }

    private static void InspectHardware(HardwareInfo hardware)
    {
        hardware.Cpu = GetWmiValue("Win32_Processor", "Name");
        hardware.Gpu = GetWmiValue("Win32_VideoController", "Name");
        hardware.Motherboard = $"{GetWmiValue("Win32_BaseBoard", "Manufacturer")} {GetWmiValue("Win32_BaseBoard", "Product")}".Trim();
        hardware.OperatingSystem = GetWmiValue("Win32_OperatingSystem", "Caption");

        var ramKb = GetWmiValue("Win32_ComputerSystem", "TotalPhysicalMemory");
        if (ulong.TryParse(ramKb, out var ramBytes))
        {
            hardware.RamBytes = ramBytes;
        }
    }

    private static void InspectSoftware(SoftwareInfo software)
    {
        software.SteamPath = ReadRegistryValue(Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath");
        software.SteamInstalled = !string.IsNullOrWhiteSpace(software.SteamPath) && Directory.Exists(software.SteamPath);

        if (software.SteamInstalled && !string.IsNullOrWhiteSpace(software.SteamPath))
        {
            var steamVrPath = Path.Combine(software.SteamPath, "steamapps", "common", "SteamVR");
            software.SteamVrInstalled = Directory.Exists(steamVrPath);
            software.SteamVrPath = steamVrPath;
        }

        var oculusPath = @"C:\Program Files\Oculus\Support\oculus-client";
        software.MetaQuestLinkInstalled = Directory.Exists(oculusPath);
        software.MetaQuestLinkPath = oculusPath;

        software.OpenXrRuntimePath = ReadRegistryValue(Registry.LocalMachine, @"SOFTWARE\Khronos\OpenXR\1", "ActiveRuntime")
            ?? ReadRegistryValue(Registry.CurrentUser, @"SOFTWARE\Khronos\OpenXR\1", "ActiveRuntime");
        software.OpenXrRuntimeDetected = !string.IsNullOrWhiteSpace(software.OpenXrRuntimePath);
    }

    private static IEnumerable<GameInfo> InspectSteamGames()
    {
        var steamPath = ReadRegistryValue(Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath");
        if (string.IsNullOrWhiteSpace(steamPath))
        {
            yield break;
        }

        var libraryFolders = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(libraryFolders))
        {
            yield break;
        }

        var content = File.ReadAllText(libraryFolders);
        var matches = Regex.Matches(content, @"""path""\s+""([^""]+)""");

        foreach (Match match in matches)
        {
            var libraryPath = match.Groups[1].Value.Replace("\\\\", "\\");
            var manifests = Directory.GetFiles(Path.Combine(libraryPath, "steamapps"), "appmanifest_*.acf");

            foreach (var manifest in manifests)
            {
                var manifestContent = File.ReadAllText(manifest);
                var appIdMatch = Regex.Match(manifestContent, @"""appid""\s+""(\d+)""");
                var nameMatch = Regex.Match(manifestContent, @"""name""\s+""([^""]+)""");
                var installDirMatch = Regex.Match(manifestContent, @"""installdir""\s+""([^""]+)""");

                if (appIdMatch.Success && nameMatch.Success)
                {
                    yield return new GameInfo
                    {
                        Id = appIdMatch.Groups[1].Value,
                        Name = nameMatch.Groups[1].Value,
                        InstallDir = installDirMatch.Success
                            ? Path.Combine(libraryPath, "steamapps", "common", installDirMatch.Groups[1].Value)
                            : string.Empty
                    };
                }
            }
        }
    }

    private static string? ReadRegistryValue(RegistryKey baseKey, string path, string valueName)
    {
        using var key = baseKey.OpenSubKey(path);
        return key?.GetValue(valueName)?.ToString();
    }

    private static string GetWmiValue(string className, string propertyName)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {className}");
            foreach (var obj in searcher.Get().Cast<ManagementObject>())
            {
                var value = obj[propertyName]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }
        }
        catch
        {
            // WMI puede fallar en algunos entornos; no bloqueamos el inspector.
        }

        return string.Empty;
    }
}
