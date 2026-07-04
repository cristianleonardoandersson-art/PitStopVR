namespace PitStopVR.Core.Models;

public sealed class MachineProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public HardwareInfo Hardware { get; set; } = new();
    public SoftwareInfo Software { get; set; } = new();
    public List<GameInfo> Games { get; set; } = new();
}

public sealed class HardwareInfo
{
    public string Cpu { get; set; } = string.Empty;
    public string Gpu { get; set; } = string.Empty;
    public ulong RamBytes { get; set; }
    public string Motherboard { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
}

public sealed class SoftwareInfo
{
    public bool SteamInstalled { get; set; }
    public string? SteamPath { get; set; }
    public bool SteamVrInstalled { get; set; }
    public string? SteamVrPath { get; set; }
    public bool MetaQuestLinkInstalled { get; set; }
    public string? MetaQuestLinkPath { get; set; }
    public bool OpenXrRuntimeDetected { get; set; }
    public string? OpenXrRuntimePath { get; set; }
}

public sealed class GameInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string InstallDir { get; set; } = string.Empty;
    public string Source { get; set; } = "Steam";
}
