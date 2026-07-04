using System.Text.Json;

namespace PitStopVR.Inspector.Games;

public sealed class DetectionConfigLoader
{
    private readonly string _knowledgePath;

    public DetectionConfigLoader(string knowledgePath)
    {
        _knowledgePath = knowledgePath;
    }

    public DetectionConfig Load()
    {
        var filePath = Path.Combine(_knowledgePath, "rules", "detection.json");
        if (!File.Exists(filePath))
        {
            return new DetectionConfig();
        }

        var content = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<DetectionConfig>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }) ?? new DetectionConfig();
    }
}

public sealed class DetectionConfig
{
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DetectionRules Detection { get; set; } = new();
    public GameDetectionConfig Games { get; set; } = new();
}

public sealed class DetectionRules
{
    public SoftwareDetectionRule Steam { get; set; } = new();
    public SoftwareDetectionRule SteamVr { get; set; } = new();
    public SoftwareDetectionRule MetaQuestLink { get; set; } = new();
    public SoftwareDetectionRule OculusDebugTool { get; set; } = new();
    public SoftwareDetectionRule OpenXr { get; set; } = new();
}

public sealed class SoftwareDetectionRule
{
    public List<string> RegistryPaths { get; set; } = new();
    public string RegistryValue { get; set; } = string.Empty;
    public string DefaultInstallPath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string ConfigFile { get; set; } = string.Empty;
    public string Executable { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public List<string> Processes { get; set; } = new();
    public string ActiveRuntimeValue { get; set; } = string.Empty;
}

public sealed class GameDetectionConfig
{
    public List<SteamAppId> SteamAppIds { get; set; } = new();
    public List<CustomFolderConfig> CustomFolders { get; set; } = new();
}

public sealed class SteamAppId
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
