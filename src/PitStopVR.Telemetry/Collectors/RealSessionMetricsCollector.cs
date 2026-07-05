using PitStopVR.Core.Models;
using PitStopVR.Knowledge.Models;
using PitStopVR.Telemetry.Collectors.Adapters;
using PitStopVR.Telemetry.Models;
using System.Text.Json;

namespace PitStopVR.Telemetry.Collectors;

public sealed class RealSessionMetricsCollector : ISessionMetricsCollector, IDisposable
{
    private readonly OpenVRMetricsAdapter _openVr = new();
    private readonly PerformanceCountersAdapter _performanceCounters = new();
    private readonly OVRMetricsToolAdapter? _ovrMetrics;
    private readonly List<RawSessionSample> _capturedSamples = new();
    private readonly System.Timers.Timer _timer = new(2000);
    private readonly SemaphoreSlim _captureFileLock = new(1, 1);
    private string? _captureFilePath;
    private bool _disposed;

    public RealSessionMetricsCollector(string? adbPath = null)
    {
        _ovrMetrics = string.IsNullOrWhiteSpace(adbPath) ? null : new OVRMetricsToolAdapter(adbPath);
    }

    public bool IsAvailable(MachineProfile machine)
    {
        try
        {
            return _openVr.IsAvailable(machine)
                || _performanceCounters.IsAvailable(machine)
                || (_ovrMetrics?.IsAvailable(machine) ?? false);
        }
        catch
        {
            return false;
        }
    }

    public Task StartSessionAsync(
        GameInfo game,
        Profile profile,
        MachineProfile machine,
        string captureFilePath,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RealSessionMetricsCollector));
        }

        _captureFilePath = captureFilePath;
        _capturedSamples.Clear();

        _timer.Elapsed += (_, _) => CaptureTick(game, profile, machine);
        _timer.AutoReset = true;
        _timer.Start();

        _ovrMetrics?.EnableCsvCaptureAsync(cancellationToken).ConfigureAwait(false);

        CaptureTick(game, profile, machine);
        return Task.CompletedTask;
    }

    public async Task StopSessionAsync(CancellationToken cancellationToken = default)
    {
        _timer.Stop();
        _timer.Elapsed -= (_, _) => { };

        await SaveCaptureAsync(cancellationToken);
        await (_ovrMetrics?.DisableCsvCaptureAsync(cancellationToken) ?? Task.CompletedTask);
    }

    public async Task<SessionSummary> BuildSummaryAsync(
        GameInfo game,
        Profile profile,
        MachineProfile machine,
        string captureFilePath,
        CancellationToken cancellationToken = default)
    {
        List<RawSessionSample> samples;

        if (File.Exists(captureFilePath))
        {
            var json = await File.ReadAllTextAsync(captureFilePath, cancellationToken);
            samples = JsonSerializer.Deserialize<List<RawSessionSample>>(json) ?? new List<RawSessionSample>();
        }
        else
        {
            samples = _capturedSamples.ToList();
        }

        var ovrSamples = await PullOVRMetricsSamplesAsync(cancellationToken);
        if (ovrSamples.Count > 0)
        {
            samples = MergeSamples(samples, ovrSamples);
        }

        return BuildSummary(game, profile, machine, samples);
    }

    private void CaptureTick(GameInfo game, Profile profile, MachineProfile machine)
    {
        RawSessionSample? sample = null;

        try
        {
            if (_openVr.IsAvailable(machine))
            {
                sample = _openVr.CaptureSample();
            }
        }
        catch
        {
            // OpenVR no disponible; se intentará PerformanceCounters.
        }

        if (sample is null)
        {
            try
            {
                sample = _performanceCounters.CaptureSample();
            }
            catch
            {
                // Performance counters no disponibles.
            }
        }

        if (sample is not null)
        {
            _capturedSamples.Add(sample);
            _ = SaveCaptureAsync();
        }
    }

    private async Task SaveCaptureAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_captureFilePath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(_captureFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Serializa las escrituras: el timer de captura (fire-and-forget) y el guardado
        // final de StopSessionAsync pueden solaparse; sin este candado, dos escrituras
        // concurrentes al mismo archivo lanzan IOException ("used by another process").
        await _captureFileLock.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(_capturedSamples, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_captureFilePath, json, cancellationToken);
        }
        finally
        {
            _captureFileLock.Release();
        }
    }

    private async Task<List<RawSessionSample>> PullOVRMetricsSamplesAsync(CancellationToken cancellationToken)
    {
        if (_ovrMetrics is null)
        {
            return new List<RawSessionSample>();
        }

        try
        {
            var directory = Path.GetDirectoryName(_captureFilePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return new List<RawSessionSample>();
            }

            var csvPath = await _ovrMetrics.PullLatestCsvAsync(directory, cancellationToken);
            if (string.IsNullOrWhiteSpace(csvPath))
            {
                return new List<RawSessionSample>();
            }

            return _ovrMetrics.ParseCsv(csvPath);
        }
        catch
        {
            return new List<RawSessionSample>();
        }
    }

    private static List<RawSessionSample> MergeSamples(List<RawSessionSample> localSamples, List<RawSessionSample> ovrSamples)
    {
        var merged = localSamples.ToList();

        foreach (var ovr in ovrSamples)
        {
            var existing = merged.FirstOrDefault(s => Math.Abs((s.Timestamp - ovr.Timestamp).TotalSeconds) < 2);

            if (existing is null)
            {
                merged.Add(ovr);
            }
            else
            {
                if (ovr.FpsEstimate > 0) existing.FpsEstimate = ovr.FpsEstimate;
                if (ovr.CpuUsagePercent > 0) existing.CpuUsagePercent = ovr.CpuUsagePercent;
                if (ovr.GpuUsagePercent > 0) existing.GpuUsagePercent = ovr.GpuUsagePercent;
                if (ovr.ReprojectedFrames > 0) existing.ReprojectedFrames = ovr.ReprojectedFrames;
                existing.Source = SessionSourceType.OVRMetricsTool;
            }
        }

        return merged.OrderBy(s => s.Timestamp).ToList();
    }

    private static SessionSummary BuildSummary(GameInfo game, Profile profile, MachineProfile machine, List<RawSessionSample> samples)
    {
        var startedAt = samples.Count > 0 ? samples.Min(s => s.Timestamp) : DateTime.Now;
        var endedAt = samples.Count > 0 ? samples.Max(s => s.Timestamp) : DateTime.Now;
        var durationSeconds = (endedAt - startedAt).TotalSeconds;

        var configuredBitrate = profile.Odt.BitrateMbps > 0 ? profile.Odt.BitrateMbps : 150;
        var totalExpectedFrames = samples.Count > 0 ? samples.Count * 2 * Math.Max(profile.SteamVr.RefreshRate, profile.Odt.RefreshRate) : 0;

        var summary = new SessionSummary
        {
            GameId = game.Id,
            GameName = game.Name,
            ProfileName = profile.Name,
            StartedAt = startedAt,
            DurationSeconds = durationSeconds,
            IsSimulated = false,
            SourceType = DetermineDominantSource(samples),
            ConfiguredBitrateMbps = configuredBitrate,
            Samples = samples.Select(MapToSessionSample).ToList()
        };

        if (samples.Count > 0)
        {
            summary.AvgFps = Math.Round(samples.Where(s => s.FpsEstimate > 0).Select(s => s.FpsEstimate).DefaultIfEmpty().Average(), 1);
            summary.MinFps = Math.Round(samples.Where(s => s.FpsEstimate > 0).Select(s => s.FpsEstimate).DefaultIfEmpty().Min(), 1);
            summary.AvgGpuUsagePercent = Math.Round(samples.Where(s => s.GpuUsagePercent > 0).Select(s => s.GpuUsagePercent).DefaultIfEmpty().Average(), 1);
            summary.MaxGpuUsagePercent = Math.Round(samples.Where(s => s.GpuUsagePercent > 0).Select(s => s.GpuUsagePercent).DefaultIfEmpty().Max(), 1);
            summary.AvgCpuUsagePercent = Math.Round(samples.Where(s => s.CpuUsagePercent > 0).Select(s => s.CpuUsagePercent).DefaultIfEmpty().Average(), 1);
            summary.MaxCpuUsagePercent = Math.Round(samples.Where(s => s.CpuUsagePercent > 0).Select(s => s.CpuUsagePercent).DefaultIfEmpty().Max(), 1);
            summary.AvgBitrateMbps = Math.Round(samples.Where(s => s.BitrateMbps > 0).Select(s => s.BitrateMbps).DefaultIfEmpty().Average(), 1);
            summary.AvgWifiSignalPercent = Math.Round(samples.Where(s => s.WifiSignalPercent > 0).Select(s => s.WifiSignalPercent).DefaultIfEmpty().Average(), 1);

            var droppedFrames = samples.Sum(s => s.DroppedFrames);
            var reprojectedFrames = samples.Sum(s => s.ReprojectedFrames);
            summary.DroppedFramesPercent = totalExpectedFrames > 0 ? Math.Round(droppedFrames / (double)totalExpectedFrames * 100, 1) : 0;
            summary.ReprojectedFramesPercent = totalExpectedFrames > 0 ? Math.Round(reprojectedFrames / (double)totalExpectedFrames * 100, 1) : 0;
        }

        return summary;
    }

    private static SessionSourceType DetermineDominantSource(List<RawSessionSample> samples)
    {
        if (samples.Count == 0)
        {
            return SessionSourceType.PerformanceCounters;
        }

        // Prioriza la fuente más rica en datos cuando hay una mezcla de orígenes:
        // OVR Metrics Tool > OpenVR > Contadores de rendimiento.
        if (samples.Any(s => s.Source == SessionSourceType.OVRMetricsTool))
        {
            return SessionSourceType.OVRMetricsTool;
        }

        if (samples.Any(s => s.Source == SessionSourceType.OpenVR))
        {
            return SessionSourceType.OpenVR;
        }

        return samples
            .GroupBy(s => s.Source)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();
    }

    private static SessionSample MapToSessionSample(RawSessionSample raw)
    {
        return new SessionSample
        {
            Timestamp = raw.Timestamp,
            FpsEstimate = raw.FpsEstimate,
            GpuUsagePercent = raw.GpuUsagePercent,
            CpuUsagePercent = raw.CpuUsagePercent,
            DroppedFrames = raw.DroppedFrames,
            ReprojectedFrames = raw.ReprojectedFrames,
            BitrateMbps = raw.BitrateMbps,
            WifiSignalPercent = raw.WifiSignalPercent
        };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _timer.Stop();
        _timer.Dispose();
        _performanceCounters.Dispose();
        _openVr.Dispose();
        _captureFileLock.Dispose();
        _disposed = true;
    }
}
