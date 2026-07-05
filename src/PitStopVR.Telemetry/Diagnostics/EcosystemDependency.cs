namespace PitStopVR.Telemetry.Diagnostics;

public sealed class EcosystemDependency
{
    public EcosystemDependencyType Type { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public bool IsAvailable { get; set; }
    public string? Path { get; set; }
    public required string DownloadUrl { get; set; }
    public string? InstallHelpText { get; set; }
    public bool IsRequired { get; set; }
}
