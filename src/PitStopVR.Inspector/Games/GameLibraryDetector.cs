using PitStopVR.Core.Models;

namespace PitStopVR.Inspector.Games;

public sealed class GameLibraryDetector
{
    private readonly List<IGameDetector> _detectors;

    public GameLibraryDetector()
    {
        _detectors =
        [
            new SteamGameDetector(),
            new EpicGameDetector(),
            new OculusGameDetector(),
            new CustomGameDetector([])
        ];
    }

    public GameLibraryDetector(DetectionConfig config)
    {
        _detectors =
        [
            new SteamGameDetector(),
            new EpicGameDetector(),
            new OculusGameDetector(),
            new CustomGameDetector(config.Games.CustomFolders)
        ];
    }

    public GameLibraryDetector(IEnumerable<IGameDetector> detectors)
    {
        _detectors = detectors.ToList();
    }

    public IEnumerable<GameInfo> DetectAll()
    {
        foreach (var detector in _detectors)
        {
            if (!detector.IsAvailable())
            {
                continue;
            }

            foreach (var game in detector.Detect())
            {
                yield return game;
            }
        }
    }

    public IReadOnlyList<GameSource> GetAvailableSources()
    {
        return _detectors
            .Where(d => d.IsAvailable())
            .Select(d => d.Source)
            .ToList();
    }
}
