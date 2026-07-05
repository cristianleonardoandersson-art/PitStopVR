namespace PitStopVR.Telemetry.Configuration;

public sealed class TelemetrySettings
{
    public string? AdbPath { get; set; }
    public string? SteamVRPath { get; set; }
    public int MinSessionSeconds { get; set; } = 10;
    public bool PreferOpenVR { get; set; } = true;
    public bool PreferOVRMetricsTool { get; set; } = true;
}
