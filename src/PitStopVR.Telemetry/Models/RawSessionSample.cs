namespace PitStopVR.Telemetry.Models;

public enum SessionSourceType
{
    Simulated,
    OpenVR,
    PerformanceCounters,
    OVRMetricsTool
}

public sealed class RawSessionSample
{
    public DateTime Timestamp { get; set; }
    public double FpsEstimate { get; set; }
    public double GpuUsagePercent { get; set; }
    public double CpuUsagePercent { get; set; }
    public int DroppedFrames { get; set; }
    public int ReprojectedFrames { get; set; }
    public double BitrateMbps { get; set; }
    public double WifiSignalPercent { get; set; }
    public SessionSourceType Source { get; set; }
}
