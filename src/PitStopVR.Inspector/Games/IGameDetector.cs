using PitStopVR.Core.Models;

namespace PitStopVR.Inspector.Games;

public interface IGameDetector
{
    GameSource Source { get; }
    bool IsAvailable();
    IEnumerable<GameInfo> Detect();
}
