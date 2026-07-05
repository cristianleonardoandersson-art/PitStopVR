using PitStopVR.Core.Models;
using PitStopVR.Knowledge.Models;
using PitStopVR.Telemetry.Models;
using System.Text.Json;

namespace PitStopVR.Telemetry.Collectors;

public sealed class SimulatedSessionMetricsCollector : ISessionMetricsCollector
{
    private enum Scenario
    {
        GpuBound,
        CpuBound,
        WirelessBound,
        WeakWifiSignal,
        Balanced
    }

    private const int SampleCount = 24;

    private List<SessionSample>? _simulatedSamples;

    public bool IsAvailable(MachineProfile machine)
    {
        return true;
    }

    public Task StartSessionAsync(
        GameInfo game,
        Profile profile,
        MachineProfile machine,
        string captureFilePath,
        CancellationToken cancellationToken = default)
    {
        var summary = GenerateSummary(game, profile, machine);
        _simulatedSamples = summary.Samples;

        var directory = Path.GetDirectoryName(captureFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var rawSamples = summary.Samples.Select(MapToRaw).ToList();
        var json = JsonSerializer.Serialize(rawSamples, new JsonSerializerOptions { WriteIndented = true });
        return File.WriteAllTextAsync(captureFilePath, json, cancellationToken);
    }

    public Task StopSessionAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<SessionSummary> BuildSummaryAsync(
        GameInfo game,
        Profile profile,
        MachineProfile machine,
        string captureFilePath,
        CancellationToken cancellationToken = default)
    {
        if (_simulatedSamples is null && File.Exists(captureFilePath))
        {
            var json = File.ReadAllText(captureFilePath);
            var rawSamples = JsonSerializer.Deserialize<List<RawSessionSample>>(json) ?? new List<RawSessionSample>();
            _simulatedSamples = rawSamples.Select(MapToSessionSample).ToList();
        }

        var summary = GenerateSummary(game, profile, machine);
        summary.Samples = _simulatedSamples ?? summary.Samples;
        return Task.FromResult(summary);
    }

    private static SessionSummary GenerateSummary(GameInfo game, Profile profile, MachineProfile machine)
    {
        var random = Random.Shared;
        var scenario = PickScenario(random);
        var maxRefreshRate = Math.Max(profile.SteamVr.RefreshRate, profile.Odt.RefreshRate);
        var refreshRate = maxRefreshRate > 0 ? maxRefreshRate : 90;

        var startedAt = DateTime.Now.AddSeconds(-SampleCount * 2);
        var samples = new List<SessionSample>(SampleCount);

        double totalExpectedFrames = 0;
        double totalDroppedFrames = 0;
        double totalReprojectedFrames = 0;

        for (var i = 0; i < SampleCount; i++)
        {
            var (gpu, cpu, dropRatio, reprojRatio, bitrateRatio, wifiSignal) = GetScenarioTargets(scenario, random);

            var expectedFrames = refreshRate * 2;
            var dropped = (int)Math.Round(expectedFrames * dropRatio);
            var reprojected = (int)Math.Round(expectedFrames * reprojRatio);
            var fps = refreshRate * (1 - Math.Min(0.6, dropRatio + reprojRatio * 0.5));

            var configuredBitrate = profile.Odt.BitrateMbps > 0 ? profile.Odt.BitrateMbps : 150;
            var bitrate = configuredBitrate * bitrateRatio;

            samples.Add(new SessionSample
            {
                Timestamp = startedAt.AddSeconds(i * 2),
                FpsEstimate = Math.Round(fps, 1),
                GpuUsagePercent = Math.Round(gpu, 1),
                CpuUsagePercent = Math.Round(cpu, 1),
                DroppedFrames = dropped,
                ReprojectedFrames = reprojected,
                BitrateMbps = Math.Round(bitrate, 1),
                WifiSignalPercent = Math.Round(wifiSignal, 1)
            });

            totalExpectedFrames += expectedFrames;
            totalDroppedFrames += dropped;
            totalReprojectedFrames += reprojected;
        }

        var summary = new SessionSummary
        {
            GameId = game.Id,
            GameName = game.Name,
            ProfileName = profile.Name,
            StartedAt = startedAt,
            DurationSeconds = SampleCount * 2,
            IsSimulated = true,
            SourceType = SessionSourceType.Simulated,
            Samples = samples,
            AvgFps = Math.Round(samples.Average(s => s.FpsEstimate), 1),
            MinFps = Math.Round(samples.Min(s => s.FpsEstimate), 1),
            AvgGpuUsagePercent = Math.Round(samples.Average(s => s.GpuUsagePercent), 1),
            MaxGpuUsagePercent = Math.Round(samples.Max(s => s.GpuUsagePercent), 1),
            AvgCpuUsagePercent = Math.Round(samples.Average(s => s.CpuUsagePercent), 1),
            MaxCpuUsagePercent = Math.Round(samples.Max(s => s.CpuUsagePercent), 1),
            DroppedFramesPercent = totalExpectedFrames > 0 ? Math.Round(totalDroppedFrames / totalExpectedFrames * 100, 1) : 0,
            ReprojectedFramesPercent = totalExpectedFrames > 0 ? Math.Round(totalReprojectedFrames / totalExpectedFrames * 100, 1) : 0,
            ConfiguredBitrateMbps = profile.Odt.BitrateMbps > 0 ? profile.Odt.BitrateMbps : 150,
            AvgBitrateMbps = Math.Round(samples.Average(s => s.BitrateMbps), 1),
            AvgWifiSignalPercent = Math.Round(samples.Average(s => s.WifiSignalPercent), 1)
        };

        return summary;
    }

    private static Scenario PickScenario(Random random)
    {
        var roll = random.NextDouble();
        return roll switch
        {
            < 0.35 => Scenario.GpuBound,
            < 0.60 => Scenario.CpuBound,
            < 0.80 => Scenario.WirelessBound,
            < 0.90 => Scenario.WeakWifiSignal,
            _ => Scenario.Balanced
        };
    }

    private static (double gpu, double cpu, double dropRatio, double reprojRatio, double bitrateRatio, double wifiSignal) GetScenarioTargets(Scenario scenario, Random random)
    {
        double Noise(double range) => (random.NextDouble() - 0.5) * range;

        return scenario switch
        {
            Scenario.GpuBound => (
                gpu: 95 + Noise(6),
                cpu: 55 + Noise(20),
                dropRatio: 0.06 + Noise(0.04),
                reprojRatio: 0.03 + Noise(0.02),
                bitrateRatio: 0.95 + Noise(0.06),
                wifiSignal: 90 + Noise(10)),

            Scenario.CpuBound => (
                gpu: 60 + Noise(20),
                cpu: 95 + Noise(6),
                dropRatio: 0.05 + Noise(0.04),
                reprojRatio: 0.02 + Noise(0.02),
                bitrateRatio: 0.95 + Noise(0.06),
                wifiSignal: 90 + Noise(10)),

            Scenario.WirelessBound => (
                gpu: 70 + Noise(16),
                cpu: 65 + Noise(16),
                dropRatio: 0.05 + Noise(0.03),
                reprojRatio: 0.18 + Noise(0.08),
                bitrateRatio: 0.75 + Noise(0.1),
                wifiSignal: 60 + Noise(16)),

            Scenario.WeakWifiSignal => (
                gpu: 65 + Noise(16),
                cpu: 60 + Noise(16),
                dropRatio: 0.08 + Noise(0.05),
                reprojRatio: 0.15 + Noise(0.08),
                bitrateRatio: 0.5 + Noise(0.12),
                wifiSignal: 32 + Noise(14)),

            _ => (
                gpu: 70 + Noise(14),
                cpu: 65 + Noise(14),
                dropRatio: 0.01 + Noise(0.015),
                reprojRatio: 0.01 + Noise(0.015),
                bitrateRatio: 0.97 + Noise(0.05),
                wifiSignal: 92 + Noise(8))
        };
    }

    private static RawSessionSample MapToRaw(SessionSample sample)
    {
        return new RawSessionSample
        {
            Timestamp = sample.Timestamp,
            FpsEstimate = sample.FpsEstimate,
            GpuUsagePercent = sample.GpuUsagePercent,
            CpuUsagePercent = sample.CpuUsagePercent,
            DroppedFrames = sample.DroppedFrames,
            ReprojectedFrames = sample.ReprojectedFrames,
            BitrateMbps = sample.BitrateMbps,
            WifiSignalPercent = sample.WifiSignalPercent,
            Source = SessionSourceType.Simulated
        };
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
}
