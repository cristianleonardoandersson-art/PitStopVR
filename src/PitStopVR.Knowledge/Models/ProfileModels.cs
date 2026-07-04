using System.Text.Json;
using System.Text.Json.Serialization;

namespace PitStopVR.Knowledge.Models;

public sealed class ProfileSet
{
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Profile> Profiles { get; set; } = new();
}

public sealed class Profile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SteamVrSettings SteamVr { get; set; } = new();
    public OpenXrSettings OpenXr { get; set; } = new();
    public OdtSettings Odt { get; set; } = new();
    public GameSettings Game { get; set; } = new();
}

public sealed class SteamVrSettings
{
    public double ResolutionPerEye { get; set; }
    public bool MotionSmoothing { get; set; }
    public int RefreshRate { get; set; }
}

public sealed class OpenXrSettings
{
    public string PreferredRuntime { get; set; } = string.Empty;
}

public sealed class OdtSettings
{
    public int BitrateMbps { get; set; }
    public bool AdaptiveGpuScale { get; set; }
    public int FovTangent { get; set; }
    public double EncoderResolution { get; set; }
}

public sealed class GameSettings
{
    public string LaunchArgs { get; set; } = string.Empty;
}
