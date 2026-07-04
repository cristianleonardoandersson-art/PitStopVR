using PitStopVR.Core.Models;
using System.Text.Json;

namespace PitStopVR.Inspector.Games;

public sealed class EpicGameDetector : IGameDetector
{
    private const string EpicManifestPath = @"C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests";

    public GameSource Source => GameSource.Epic;

    public bool IsAvailable()
    {
        return Directory.Exists(EpicManifestPath);
    }

    public IEnumerable<GameInfo> Detect()
    {
        var games = new List<GameInfo>();
        if (!IsAvailable())
        {
            return games;
        }

        var manifestFiles = Directory.GetFiles(EpicManifestPath, "*.item");
        foreach (var manifestFile in manifestFiles)
        {
            try
            {
                var content = File.ReadAllText(manifestFile);
                var manifest = JsonSerializer.Deserialize<EpicManifest>(content);
                if (manifest is null || !manifest.IsApplication)
                {
                    continue;
                }

                games.Add(new GameInfo
                {
                    Id = manifest.AppName,
                    Name = manifest.DisplayName,
                    InstallDir = manifest.InstallLocation,
                    Source = GameSource.Epic,
                    ExecutablePath = manifest.LaunchExecutable
                });
            }
            catch
            {
                // Ignorar manifiestos corruptos o con formato inesperado.
            }
        }

        return games;
    }

    private sealed class EpicManifest
    {
        public string AppName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string InstallLocation { get; set; } = string.Empty;
        public string LaunchExecutable { get; set; } = string.Empty;
        public bool IsApplication { get; set; }
    }
}
