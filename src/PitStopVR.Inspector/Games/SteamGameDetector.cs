using Microsoft.Win32;
using PitStopVR.Core.Models;
using System.Text.RegularExpressions;

namespace PitStopVR.Inspector.Games;

public sealed class SteamGameDetector : IGameDetector
{
    public GameSource Source => GameSource.Steam;

    public bool IsAvailable()
    {
        return !string.IsNullOrWhiteSpace(GetSteamPath());
    }

    public IEnumerable<GameInfo> Detect()
    {
        var steamPath = GetSteamPath();
        if (string.IsNullOrWhiteSpace(steamPath))
        {
            yield break;
        }

        var libraryFoldersFile = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(libraryFoldersFile))
        {
            yield break;
        }

        var content = File.ReadAllText(libraryFoldersFile);
        var libraryPaths = ExtractLibraryPaths(content);

        foreach (var libraryPath in libraryPaths)
        {
            if (!Directory.Exists(libraryPath))
            {
                continue;
            }

            var steamAppsPath = Path.Combine(libraryPath, "steamapps");
            if (!Directory.Exists(steamAppsPath))
            {
                continue;
            }

            var manifests = Directory.GetFiles(steamAppsPath, "appmanifest_*.acf");
            foreach (var manifest in manifests)
            {
                var game = ParseManifest(manifest, libraryPath);
                if (game is not null)
                {
                    yield return game;
                }
            }
        }
    }

    private static string? GetSteamPath()
    {
        return ReadRegistryValue(Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath")
            ?? ReadRegistryValue(Registry.LocalMachine, @"Software\Valve\Steam", "SteamPath");
    }

    private static string? ReadRegistryValue(RegistryKey baseKey, string path, string valueName)
    {
        using var key = baseKey.OpenSubKey(path);
        var value = key?.GetValue(valueName)?.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Replace('/', '\\');
    }

    private static IEnumerable<string> ExtractLibraryPaths(string vdfContent)
    {
        var matches = Regex.Matches(vdfContent, @"""path""\s+""([^""]+)""");
        foreach (Match match in matches)
        {
            var path = match.Groups[1].Value.Replace("\\\\", "\\");
            if (Directory.Exists(path))
            {
                yield return path;
            }
        }
    }

    private static GameInfo? ParseManifest(string manifestPath, string libraryPath)
    {
        var content = File.ReadAllText(manifestPath);

        var appIdMatch = Regex.Match(content, @"""appid""\s+""(\d+)""");
        var nameMatch = Regex.Match(content, @"""name""\s+""([^""]+)""");
        var installDirMatch = Regex.Match(content, @"""installdir""\s+""([^""]+)""");

        if (!appIdMatch.Success || !nameMatch.Success || !installDirMatch.Success)
        {
            return null;
        }

        var installDir = Path.Combine(libraryPath, "steamapps", "common", installDirMatch.Groups[1].Value);

        return new GameInfo
        {
            Id = appIdMatch.Groups[1].Value,
            Name = nameMatch.Groups[1].Value,
            InstallDir = installDir,
            Source = GameSource.Steam,
            ExecutablePath = TryFindExecutable(installDir)
        };
    }

    private static string TryFindExecutable(string installDir)
    {
        if (!Directory.Exists(installDir))
        {
            return string.Empty;
        }

        var executables = Directory.GetFiles(installDir, "*.exe", SearchOption.TopDirectoryOnly);
        return executables.FirstOrDefault() ?? string.Empty;
    }
}
