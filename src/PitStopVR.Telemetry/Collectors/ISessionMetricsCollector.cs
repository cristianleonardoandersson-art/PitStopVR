using PitStopVR.Core.Models;
using PitStopVR.Knowledge.Models;
using PitStopVR.Telemetry.Models;

namespace PitStopVR.Telemetry.Collectors;

public interface ISessionMetricsCollector
{
    bool IsAvailable(MachineProfile machine);

    Task StartSessionAsync(
        GameInfo game,
        Profile profile,
        MachineProfile machine,
        string captureFilePath,
        CancellationToken cancellationToken = default);

    Task StopSessionAsync(CancellationToken cancellationToken = default);

    Task<SessionSummary> BuildSummaryAsync(
        GameInfo game,
        Profile profile,
        MachineProfile machine,
        string captureFilePath,
        CancellationToken cancellationToken = default);
}
