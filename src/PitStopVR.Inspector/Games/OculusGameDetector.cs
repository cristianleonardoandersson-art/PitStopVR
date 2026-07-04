using PitStopVR.Core.Models;
using System.Text.Json;

namespace PitStopVR.Inspector.Games;

public sealed class OculusGameDetector : IGameDetector
{
    private const string OculusManifestPath = @"C:\Program Files\Oculus\Software\Manifests";

    public GameSource Source => GameSource.Oculus;

    public bool IsAvailable()
    {
        return Directory.Exists(OculusManifestPath);
    }

    public IEnumerable<GameInfo> Detect()
    {
        var games = new List<GameInfo>();
        if (!IsAvailable())
        {
            return games;
        }

        var manifestFiles = Directory.GetFiles(OculusManifestPath, "*.json");
        foreach (var manifestFile in manifestFiles)
        {
            try
            {
                var content = File.ReadAllText(manifestFile);
                var manifest = JsonSerializer.Deserialize<OculusManifest>(content);
                if (manifest is null)
                {
                    continue;
                }

                games.Add(new GameInfo
                {
                    Id = manifest.CanonicalName,
                    Name = manifest.LaunchFile,
                    InstallDir = manifest.InstallPath,
                    Source = GameSource.Oculus,
                    ExecutablePath = Path.Combine(manifest.InstallPath, manifest.LaunchFile)
                });
            }
            catch
            {
                // Ignorar manifiestos corruptos.
            }
        }

        return games;
    }

    private sealed class OculusManifest
    {
        public string CanonicalName { get; set; } = string.Empty;
        public string LaunchFile { get; set; } = string.Empty;
        public string InstallPath { get; set; } = string.Empty;
    }
}
