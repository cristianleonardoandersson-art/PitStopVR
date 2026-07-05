using PitStopVR.Core.Models;
using PitStopVR.Knowledge.Models;
using PitStopVR.Telemetry.Analysis;
using PitStopVR.Telemetry.Collectors;
using PitStopVR.Telemetry.Collectors.Adapters;
using PitStopVR.Telemetry.Collectors.Adapters.OpenVR;
using PitStopVR.Telemetry.Configuration;
using PitStopVR.Telemetry.Diagnostics;
using PitStopVR.Telemetry.Models;

namespace PitStopVR.Tests;

public class RealTelemetryTests : IDisposable
{
    private readonly string _tempAppDataPath;

    public RealTelemetryTests()
    {
        _tempAppDataPath = Path.Combine(Path.GetTempPath(), "PitStopVRTests_RealTelemetry_" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempAppDataPath))
        {
            Directory.Delete(_tempAppDataPath, recursive: true);
        }
    }

    private static GameInfo TestGame() => new() { Id = "game-1", Name = "Juego Test" };

    private static Profile TestProfile() => new()
    {
        Name = "Perfil Test",
        SteamVr = new SteamVrSettings { RefreshRate = 90 },
        Odt = new OdtSettings { BitrateMbps = 150, RefreshRate = 90 }
    };

    private static MachineProfile TestMachine() => new()
    {
        Software = new SoftwareInfo
        {
            SteamInstalled = true,
            SteamVrInstalled = true
        }
    };

    [Fact]
    public void AdbLocator_FindsFakeAdbInTempFolder()
    {
        var locator = new AdbLocator();

        var originalPath = Environment.GetEnvironmentVariable("ANDROID_HOME");
        var fakeSdk = Path.Combine(_tempAppDataPath, "fake-android-sdk");
        var fakeAdb = Path.Combine(fakeSdk, "platform-tools", "adb.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(fakeAdb)!);
        File.WriteAllText(fakeAdb, "fake adb");

        try
        {
            Environment.SetEnvironmentVariable("ANDROID_HOME", fakeSdk);
            var found = locator.FindAdb();
            Assert.Equal(fakeAdb, found);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ANDROID_HOME", originalPath);
        }
    }

    [Fact]
    public void PerformanceCountersAdapter_IsAvailable_ReturnsTrueOnWindows()
    {
        var adapter = new PerformanceCountersAdapter();
        var available = adapter.IsAvailable(TestMachine());
        Assert.True(available);
    }

    [Fact]
    public void PerformanceCountersAdapter_CaptureSample_ReturnsCpuValue()
    {
        var adapter = new PerformanceCountersAdapter();
        var sample = adapter.CaptureSample();

        Assert.True(sample.CpuUsagePercent >= 0);
        Assert.True(sample.CpuUsagePercent <= 100);
        Assert.Equal(SessionSourceType.PerformanceCounters, sample.Source);
    }

    [Fact]
    public void OpenVRNative_FindsNoDll_WhenSteamVRNotInstalled()
    {
        var path = PitStopVR.Telemetry.Collectors.Adapters.OpenVR.OpenVRNative.FindOpenVRApiPath();
        Assert.Null(path);
    }

    [Fact]
    public void OpenVRMetricsAdapter_IsAvailable_ReturnsFalse_WhenSteamVRNotInstalled()
    {
        var adapter = new OpenVRMetricsAdapter();
        var available = adapter.IsAvailable(TestMachine());
        Assert.False(available);
    }

    [Fact]
    public void OVRMetricsToolAdapter_ParseCsv_ExtractsFrameRateAndCpuGpu()
    {
        var csvPath = Path.Combine(_tempAppDataPath, "ovr_metrics.csv");
        Directory.CreateDirectory(_tempAppDataPath);
        File.WriteAllText(csvPath, @"average_frame_rate,cpu_utilization_percentage,gpu_utilization_percentage,stale_frame_count
72.5,45.2,60.1,2
70.1,48.0,58.3,1");

        var adapter = new OVRMetricsToolAdapter();
        var samples = adapter.ParseCsv(csvPath);

        Assert.Equal(2, samples.Count);
        Assert.Equal(72.5, samples[0].FpsEstimate);
        Assert.Equal(45.2, samples[0].CpuUsagePercent);
        Assert.Equal(60.1, samples[0].GpuUsagePercent);
        Assert.Equal(2, samples[0].ReprojectedFrames);
        Assert.Equal(SessionSourceType.OVRMetricsTool, samples[0].Source);
    }

    [Fact]
    public async Task SessionRecorder_SimulatedPath_WritesCaptureFileAndBuildsSummary()
    {
        var recorder = new SessionRecorder(_tempAppDataPath, simulated: true);
        var game = TestGame();
        var profile = TestProfile();
        var machine = TestMachine();

        await recorder.StartSessionAsync(game, profile, machine);
        var summary = await recorder.StopSessionAsync();

        Assert.NotNull(summary);
        Assert.True(summary!.IsSimulated);
        Assert.NotEmpty(summary.Samples);
        Assert.Equal(game.Name, summary.GameName);

        var captureFile = recorder.GetCaptureFilePath();
        Assert.True(File.Exists(captureFile));
    }

    [Fact]
    public async Task SessionRecorder_RealPath_FallsBackToSimulatedWhenNoHardware()
    {
        var recorder = new SessionRecorder(_tempAppDataPath, simulated: false);
        var game = TestGame();
        var profile = TestProfile();
        var machine = new MachineProfile(); // Sin SteamVR ni ADB

        await recorder.StartSessionAsync(game, profile, machine);
        var summary = await recorder.StopSessionAsync();

        Assert.NotNull(summary);
        Assert.NotEmpty(summary!.Samples);
    }

    [Fact]
    public void BottleneckAnalyzer_StillWorks_WithPartialData()
    {
        var summary = new SessionSummary
        {
            AvgCpuUsagePercent = 65,
            AvgGpuUsagePercent = 70,
            ReprojectedFramesPercent = 1,
            AvgWifiSignalPercent = 90,
            ConfiguredBitrateMbps = 150,
            AvgBitrateMbps = 145
        };

        var result = BottleneckAnalyzer.Analyze(summary);

        Assert.Equal(BottleneckType.None, result.Type);
    }

    [Fact]
    public void TelemetrySettingsStore_SaveAndLoad_RoundTripsValues()
    {
        Directory.CreateDirectory(_tempAppDataPath);
        var store = new TelemetrySettingsStore(_tempAppDataPath);
        var settings = new TelemetrySettings
        {
            AdbPath = @"C:\fake\adb.exe",
            SteamVRPath = @"C:\fake\SteamVR",
            MinSessionSeconds = 30,
            PreferOpenVR = false,
            PreferOVRMetricsTool = false
        };

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal(settings.AdbPath, loaded.AdbPath);
        Assert.Equal(settings.SteamVRPath, loaded.SteamVRPath);
        Assert.Equal(settings.MinSessionSeconds, loaded.MinSessionSeconds);
        Assert.Equal(settings.PreferOpenVR, loaded.PreferOpenVR);
        Assert.Equal(settings.PreferOVRMetricsTool, loaded.PreferOVRMetricsTool);
    }

    [Fact]
    public void TelemetrySettingsStore_Load_ReturnsDefaults_WhenFileMissing()
    {
        var store = new TelemetrySettingsStore(_tempAppDataPath);

        var loaded = store.Load();

        Assert.Null(loaded.AdbPath);
        Assert.Null(loaded.SteamVRPath);
        Assert.Equal(10, loaded.MinSessionSeconds);
        Assert.True(loaded.PreferOpenVR);
        Assert.True(loaded.PreferOVRMetricsTool);
    }

    [Fact]
    public void TelemetrySettingsStore_Load_ReturnsDefaults_WhenFileIsCorrupted()
    {
        Directory.CreateDirectory(_tempAppDataPath);
        var settingsPath = Path.Combine(_tempAppDataPath, "settings.json");
        File.WriteAllText(settingsPath, "{ esto no es json valido");

        var store = new TelemetrySettingsStore(_tempAppDataPath);
        var loaded = store.Load();

        Assert.Null(loaded.AdbPath);
        Assert.Equal(10, loaded.MinSessionSeconds);
    }

    [Theory]
    [InlineData(SessionSourceType.OpenVR, "SteamVR / OpenVR")]
    [InlineData(SessionSourceType.OVRMetricsTool, "OVR Metrics Tool (Meta Quest)")]
    [InlineData(SessionSourceType.PerformanceCounters, "Contadores de rendimiento de Windows")]
    [InlineData(SessionSourceType.Simulated, "Simulación")]
    public void SessionSummary_DataSourceLabel_ReflectsSourceType(SessionSourceType sourceType, string expectedLabel)
    {
        var summary = new SessionSummary { SourceType = sourceType };

        Assert.Equal(expectedLabel, summary.DataSourceLabel);
    }

    [Fact]
    public async Task EcosystemCheckService_ReportsAdbNotAvailable_WhenAdbMissing()
    {
        var settings = new TelemetrySettings();
        var locator = new FakeAdbLocator(null);
        var service = new EcosystemCheckService(settings, locator, _ => new OVRMetricsToolAdapter(_), _ => null);

        var dependencies = await service.CheckAllAsync(TestMachine());

        var adbDependency = dependencies.FirstOrDefault(d => d.Type == EcosystemDependencyType.ADB);
        Assert.NotNull(adbDependency);
        Assert.False(adbDependency!.IsAvailable);
        Assert.Equal("https://developer.android.com/studio/releases/platform-tools", adbDependency.DownloadUrl);
    }

    [Fact]
    public async Task EcosystemCheckService_ReportsSteamVRAvailable_WhenManualPathConfigured()
    {
        Directory.CreateDirectory(_tempAppDataPath);
        var fakeSteamDir = Path.Combine(_tempAppDataPath, "SteamVR");
        var fakeDll = Path.Combine(fakeSteamDir, "bin", "win64", "openvr_api.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(fakeDll)!);
        File.WriteAllText(fakeDll, "fake openvr");

        var settings = new TelemetrySettings { SteamVRPath = fakeSteamDir };
        var service = new EcosystemCheckService(settings, new FakeAdbLocator(null), _ => new OVRMetricsToolAdapter(_), _ => fakeDll);

        var dependencies = await service.CheckAllAsync(TestMachine());

        var steamVrDependency = dependencies.First(d => d.Type == EcosystemDependencyType.SteamVR);
        Assert.True(steamVrDependency.IsAvailable);
        Assert.Equal(fakeDll, steamVrDependency.Path);
    }

    [Fact]
    public async Task EcosystemCheckService_WhenAdbAvailable_ChecksOVRMetricsAndQuestConnection()
    {
        Directory.CreateDirectory(_tempAppDataPath);
        var fakeAdb = Path.Combine(_tempAppDataPath, "adb.exe");
        File.WriteAllText(fakeAdb, "fake adb");

        var settings = new TelemetrySettings { AdbPath = fakeAdb };
        var locator = new FakeAdbLocator(fakeAdb);
        var service = new EcosystemCheckService(
            settings,
            locator,
            _ => new FakeOVRMetricsToolAdapter(_),
            _ => null,
            (_, _) => Task.FromResult(true));

        var dependencies = await service.CheckAllAsync(TestMachine());

        var ovrDependency = dependencies.FirstOrDefault(d => d.Type == EcosystemDependencyType.OVRMetricsTool);
        var questDependency = dependencies.FirstOrDefault(d => d.Type == EcosystemDependencyType.QuestConnected);
        Assert.NotNull(ovrDependency);
        Assert.NotNull(questDependency);
        Assert.True(ovrDependency!.IsAvailable);
        Assert.True(questDependency!.IsAvailable);
    }
}

internal sealed class FakeAdbLocator : AdbLocator
{
    private readonly string? _result;

    public FakeAdbLocator(string? result)
    {
        _result = result;
    }

    public override string? FindAdb() => _result;
}

internal sealed class FakeOVRMetricsToolAdapter : OVRMetricsToolAdapter
{
    public FakeOVRMetricsToolAdapter(string? adbPath = null) : base(adbPath)
    {
    }

    public override bool IsAvailable(Core.Models.MachineProfile machine) => true;
}
