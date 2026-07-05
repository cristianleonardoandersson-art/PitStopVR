using System.Text.Json;
using System.Text.Json.Serialization;

namespace PitStopVR.Telemetry.Models;

public sealed class SessionSummary
{
    public const string FileExtension = ".json";

    public string GameId { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public string ProfileName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public double DurationSeconds { get; set; }
    public bool IsSimulated { get; set; }
    public SessionSourceType SourceType { get; set; }

    [JsonIgnore]
    public string DataSourceLabel => SourceType switch
    {
        SessionSourceType.OpenVR => "SteamVR / OpenVR",
        SessionSourceType.OVRMetricsTool => "OVR Metrics Tool (Meta Quest)",
        SessionSourceType.PerformanceCounters => "Contadores de rendimiento de Windows",
        SessionSourceType.Simulated => "Simulación",
        _ => "Desconocida"
    };

    public double AvgFps { get; set; }
    public double MinFps { get; set; }

    public double AvgGpuUsagePercent { get; set; }
    public double MaxGpuUsagePercent { get; set; }

    public double AvgCpuUsagePercent { get; set; }
    public double MaxCpuUsagePercent { get; set; }

    public double DroppedFramesPercent { get; set; }
    public double ReprojectedFramesPercent { get; set; }

    public double ConfiguredBitrateMbps { get; set; }
    public double AvgBitrateMbps { get; set; }
    public double AvgWifiSignalPercent { get; set; }

    public List<SessionSample> Samples { get; set; } = new();

    public BottleneckAnalysis Bottleneck { get; set; } = new();

    [JsonIgnore]
    public string FileNameTimestamp => StartedAt.ToString("yyyyMMdd_HHmmss");

    public void Save(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(filePath, json);
    }

    public static SessionSummary? Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<SessionSummary>(json);
    }
}
