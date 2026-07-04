using PitStopVR.Core.Models;
using System.Text.Json;

namespace PitStopVR.Inspector.Games;

public sealed class CustomGameDetector : IGameDetector
{
    private readonly IReadOnlyList<CustomFolderConfig> _folders;

    public CustomGameDetector(IEnumerable<CustomFolderConfig> folders)
    {
        _folders = folders.ToList();
    }

    public GameSource Source => GameSource.Custom;

    public bool IsAvailable()
    {
        return _folders.Any(f => Directory.Exists(f.Path));
    }

    public IEnumerable<GameInfo> Detect()
    {
        foreach (var folder in _folders)
        {
            if (!Directory.Exists(folder.Path))
            {
                continue;
            }

            var entries = folder.ScanDepth <= 0
                ? Directory.GetFiles(folder.Path, "*.exe")
                : Directory.GetFiles(folder.Path, "*.exe", SearchOption.AllDirectories);

            foreach (var executable in entries)
            {
                var fileName = Path.GetFileName(executable);
                if (ShouldExclude(fileName, folder.ExcludePatterns))
                {
                    continue;
                }

                var directory = Path.GetDirectoryName(executable) ?? folder.Path;
                var gameName = GuessGameName(directory, fileName);

                yield return new GameInfo
                {
                    Id = $"custom-{folder.Id}-{fileName}",
                    Name = gameName,
                    InstallDir = directory,
                    Source = GameSource.Custom,
                    ExecutablePath = executable
                };
            }
        }
    }

    private static bool ShouldExclude(string fileName, IEnumerable<string> patterns)
    {
        return patterns.Any(pattern =>
            fileName.Contains(pattern.Trim('*'), StringComparison.OrdinalIgnoreCase));
    }

    private static string GuessGameName(string directory, string fileName)
    {
        var directoryName = Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar));
        var executableName = Path.GetFileNameWithoutExtension(fileName);

        return !string.IsNullOrWhiteSpace(directoryName) && directoryName != fileName
            ? directoryName
            : executableName;
    }
}

public sealed class CustomFolderConfig
{
    public string Id { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ScanDepth { get; set; } = 1;
    public List<string> ExecutablePatterns { get; set; } = ["*.exe"];
    public List<string> ExcludePatterns { get; set; } = [];
}
