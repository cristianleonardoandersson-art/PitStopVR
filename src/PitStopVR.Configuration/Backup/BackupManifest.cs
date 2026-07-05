using System.Text.Json;
using System.Text.Json.Serialization;

namespace PitStopVR.Configuration.Backup;

public enum BackupEntryType
{
    File,
    Registry,
    Simulated
}

public sealed class BackupEntry
{
    public string ComponentName { get; set; } = string.Empty;
    public BackupEntryType Type { get; set; }
    public string OriginalPath { get; set; } = string.Empty;
    public string BackupPath { get; set; } = string.Empty;
}

public sealed class BackupManifest
{
    public const string FileName = "backup-manifest.json";

    public DateTime Timestamp { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public string SessionPath { get; set; } = string.Empty;
    public bool IsSimulated { get; set; }
    public List<BackupEntry> Entries { get; set; } = new();

    [JsonIgnore]
    public string ModeLabel => IsSimulated ? "Simulado" : "Real";

    public void Save(string sessionPath)
    {
        var path = Path.Combine(sessionPath, FileName);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(path, json);
    }

    public static BackupManifest? Load(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        var json = File.ReadAllText(manifestPath);
        return JsonSerializer.Deserialize<BackupManifest>(json);
    }
}
