using System.Text.Json;

namespace PitStopVR.Telemetry.Configuration;

public sealed class TelemetrySettingsStore
{
    private readonly string _filePath;

    public TelemetrySettingsStore(string appDataPath)
    {
        _filePath = Path.Combine(appDataPath, "settings.json");
    }

    public TelemetrySettings Load()
    {
        if (!File.Exists(_filePath))
        {
            return new TelemetrySettings();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize<TelemetrySettings>(json);
            return settings ?? new TelemetrySettings();
        }
        catch
        {
            return new TelemetrySettings();
        }
    }

    public void Save(TelemetrySettings settings)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_filePath, json);
    }
}
