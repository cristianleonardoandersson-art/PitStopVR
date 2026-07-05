using PitStopVR.Core.Models;
using PitStopVR.Knowledge.Models;
using PitStopVR.Telemetry.Analysis;
using PitStopVR.Telemetry.Collectors;
using PitStopVR.Telemetry.History;
using PitStopVR.Telemetry.Models;

namespace PitStopVR.Tests;

public class SessionTelemetryTests : IDisposable
{
    private readonly string _tempAppDataPath;

    public SessionTelemetryTests()
    {
        _tempAppDataPath = Path.Combine(Path.GetTempPath(), "PitStopVRTests_Telemetry_" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempAppDataPath))
        {
            Directory.Delete(_tempAppDataPath, recursive: true);
        }
    }

    private static SessionSummary BaseSummary() => new()
    {
        GameId = "game-1",
        GameName = "Juego Test",
        ProfileName = "Perfil Test",
        StartedAt = DateTime.Now,
        DurationSeconds = 48,
        IsSimulated = true,
        ConfiguredBitrateMbps = 150,
        AvgWifiSignalPercent = 90
    };

    [Fact]
    public void BottleneckAnalyzer_DetectsGpuBound()
    {
        var summary = BaseSummary();
        summary.AvgGpuUsagePercent = 95;
        summary.MaxGpuUsagePercent = 99;
        summary.AvgCpuUsagePercent = 55;
        summary.ReprojectedFramesPercent = 3;
        summary.AvgBitrateMbps = 145;

        var result = BottleneckAnalyzer.Analyze(summary);

        Assert.Equal(BottleneckType.Gpu, result.Type);
    }

    [Fact]
    public void BottleneckAnalyzer_DetectsCpuBound()
    {
        var summary = BaseSummary();
        summary.AvgGpuUsagePercent = 60;
        summary.MaxCpuUsagePercent = 99;
        summary.AvgCpuUsagePercent = 95;
        summary.ReprojectedFramesPercent = 2;
        summary.AvgBitrateMbps = 145;

        var result = BottleneckAnalyzer.Analyze(summary);

        Assert.Equal(BottleneckType.Cpu, result.Type);
    }

    [Fact]
    public void BottleneckAnalyzer_DetectsWirelessBound()
    {
        var summary = BaseSummary();
        summary.AvgGpuUsagePercent = 70;
        summary.AvgCpuUsagePercent = 65;
        summary.ReprojectedFramesPercent = 18;
        summary.AvgBitrateMbps = 112;
        summary.AvgWifiSignalPercent = 65;

        var result = BottleneckAnalyzer.Analyze(summary);

        Assert.Equal(BottleneckType.Wireless, result.Type);
    }

    [Fact]
    public void BottleneckAnalyzer_DetectsWeakWifiSignal()
    {
        var summary = BaseSummary();
        summary.AvgGpuUsagePercent = 65;
        summary.AvgCpuUsagePercent = 60;
        summary.ReprojectedFramesPercent = 15;
        summary.AvgBitrateMbps = 60;
        summary.AvgWifiSignalPercent = 32;

        var result = BottleneckAnalyzer.Analyze(summary);

        Assert.Equal(BottleneckType.WeakWifiSignal, result.Type);
    }

    [Fact]
    public void BottleneckAnalyzer_ReturnsNone_WhenBalanced()
    {
        var summary = BaseSummary();
        summary.AvgGpuUsagePercent = 70;
        summary.AvgCpuUsagePercent = 65;
        summary.ReprojectedFramesPercent = 1;
        summary.AvgBitrateMbps = 145;
        summary.AvgWifiSignalPercent = 92;

        var result = BottleneckAnalyzer.Analyze(summary);

        Assert.Equal(BottleneckType.None, result.Type);
    }

    [Fact]
    public void SessionHistoryStore_SaveAndList_RoundTrips()
    {
        var store = new SessionHistoryStore(_tempAppDataPath);
        var summary = BaseSummary();
        summary.Bottleneck = BottleneckAnalyzer.Analyze(summary);
        summary.Samples.Add(new SessionSample
        {
            Timestamp = summary.StartedAt,
            FpsEstimate = 89,
            GpuUsagePercent = 70,
            CpuUsagePercent = 65,
            DroppedFrames = 1,
            ReprojectedFrames = 1,
            BitrateMbps = 145,
            WifiSignalPercent = 90
        });

        var savedPath = store.SaveSession(summary);
        Assert.True(File.Exists(savedPath));

        var sessions = store.ListSessions();
        Assert.Single(sessions);
        Assert.Equal(summary.GameName, sessions[0].GameName);
        Assert.Single(sessions[0].Samples);

        var filtered = store.ListSessions(summary.GameId);
        Assert.Single(filtered);
    }

    [Fact]
    public void SessionHistoryStore_FilterByGame_ListsOnlyMatchingSessions()
    {
        var store = new SessionHistoryStore(_tempAppDataPath);
        var game1 = BaseSummary();
        game1.GameId = "game-1";
        game1.GameName = "Juego A";
        var game2 = BaseSummary();
        game2.GameId = "game-2";
        game2.GameName = "Juego B";

        store.SaveSession(game1);
        store.SaveSession(game2);

        var filtered = store.ListSessions("game-1");
        Assert.Single(filtered);
        Assert.Equal("Juego A", filtered[0].GameName);
    }

    [Fact]
    public void SessionHistoryStore_DeleteSession_RemovesFileAndCleansEmptyFolder()
    {
        var store = new SessionHistoryStore(_tempAppDataPath);
        var summary = BaseSummary();
        summary.GameId = "game-delete";

        var savedPath = store.SaveSession(summary);
        Assert.True(File.Exists(savedPath));

        Assert.True(store.DeleteSession(summary));
        Assert.False(File.Exists(savedPath));

        var folder = Path.GetDirectoryName(savedPath);
        Assert.False(Directory.Exists(folder));

        var allSessions = store.ListSessions();
        Assert.Empty(allSessions);
    }

    [Fact]
    public async Task SimulatedSessionMetricsCollector_GeneratesValidSamples()
    {
        ISessionMetricsCollector collector = new SimulatedSessionMetricsCollector();
        var game = new GameInfo { Id = "game-1", Name = "Juego Test" };
        var profile = new Profile
        {
            Name = "Perfil Test",
            SteamVr = new SteamVrSettings { RefreshRate = 90 },
            Odt = new OdtSettings { BitrateMbps = 150, RefreshRate = 90 }
        };
        var machine = new MachineProfile();

        var capturePath = Path.Combine(_tempAppDataPath, "capture.json");
        await collector.StartSessionAsync(game, profile, machine, capturePath);
        await collector.StopSessionAsync();
        var summary = await collector.BuildSummaryAsync(game, profile, machine, capturePath);

        Assert.NotEmpty(summary.Samples);
        Assert.True(summary.AvgFps > 0);
        Assert.All(summary.Samples, s =>
        {
            Assert.InRange(s.GpuUsagePercent, 0, 100);
            Assert.InRange(s.CpuUsagePercent, 0, 100);
            Assert.True(s.FpsEstimate > 0);
        });
    }
}
