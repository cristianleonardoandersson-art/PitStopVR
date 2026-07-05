using PitStopVR.Core.Models;
using PitStopVR.Knowledge.Models;
using PitStopVR.Telemetry.Analysis;
using PitStopVR.Telemetry.Models;

namespace PitStopVR.Telemetry.Collectors;

public sealed class SessionRecorder : IDisposable
{
    private readonly string _appDataPath;
    private readonly bool _simulated;
    private readonly string? _adbPath;
    private ISessionMetricsCollector? _collector;
    private string? _captureFilePath;
    private GameInfo? _game;
    private Profile? _profile;
    private MachineProfile? _machine;
    private bool _disposed;

    public bool IsRecording { get; private set; }

    public SessionRecorder(string appDataPath, bool simulated, string? adbPath = null)
    {
        _appDataPath = appDataPath;
        _simulated = simulated;
        _adbPath = adbPath;
    }

    public async Task StartSessionAsync(
        GameInfo game,
        Profile profile,
        MachineProfile machine,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SessionRecorder));
        }

        if (IsRecording)
        {
            throw new InvalidOperationException("Ya hay una sesión de análisis en curso.");
        }

        _game = game;
        _profile = profile;
        _machine = machine;

        var capturesFolder = Path.Combine(_appDataPath, "captures");
        Directory.CreateDirectory(capturesFolder);
        var sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _captureFilePath = Path.Combine(capturesFolder, $"{sessionId}_{game.Id}.json");

        _collector = SelectCollector(machine);
        await _collector.StartSessionAsync(game, profile, machine, _captureFilePath, cancellationToken);

        IsRecording = true;
    }

    public async Task<SessionSummary?> StopSessionAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SessionRecorder));
        }

        if (!IsRecording || _collector is null || _captureFilePath is null || _game is null || _profile is null || _machine is null)
        {
            return null;
        }

        await _collector.StopSessionAsync(cancellationToken);
        var summary = await _collector.BuildSummaryAsync(_game, _profile, _machine, _captureFilePath, cancellationToken);
        summary.Bottleneck = BottleneckAnalyzer.Analyze(summary);

        IsRecording = false;
        _collector = null;

        return summary;
    }

    public string? GetCaptureFilePath()
    {
        return _captureFilePath;
    }

    private ISessionMetricsCollector SelectCollector(MachineProfile machine)
    {
        if (_simulated)
        {
            return new SimulatedSessionMetricsCollector();
        }

        var realCollector = new RealSessionMetricsCollector(_adbPath);
        if (realCollector.IsAvailable(machine))
        {
            return realCollector;
        }

        realCollector.Dispose();
        return new SimulatedSessionMetricsCollector();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_collector is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _disposed = true;
    }
}
