namespace PitStopVR.Telemetry.Models;

public enum BottleneckType
{
    None,
    Gpu,
    Cpu,
    Wireless,
    WeakWifiSignal
}

public sealed class BottleneckAnalysis
{
    public BottleneckType Type { get; set; } = BottleneckType.None;
    public string Title { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}
