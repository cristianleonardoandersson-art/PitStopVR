using PitStopVR.Configuration.Backup;
using PitStopVR.Core.Models;
using PitStopVR.Knowledge.Models;
using System.Text.Json;

namespace PitStopVR.Configuration.Appliers;

public sealed class SteamVrApplier : IConfigurationApplier
{
    private readonly BackupManager _backupManager;

    public string Name => "SteamVR";

    public SteamVrApplier(BackupManager backupManager)
    {
        _backupManager = backupManager;
    }

    public bool CanApply(MachineProfile profile)
    {
        return profile.Software.SteamVrInstalled
            && !string.IsNullOrWhiteSpace(profile.Software.SteamVrPath);
    }

    public Task<ApplyResult> ApplyAsync(MachineProfile profile, Profile profileSettings)
    {
        var result = new ApplyResult { ComponentName = Name };

        try
        {
            var configFile = Path.Combine(profile.Software.SteamVrPath!, "config", "steamvr.vrsettings");
            if (!File.Exists(configFile))
            {
                result.Success = false;
                result.ErrorMessage = $"No se encontro el archivo de configuracion: {configFile}";
                return Task.FromResult(result);
            }

            var backup = _backupManager.BackupFile(configFile);
            result.BackupPath = backup.BackupPath;

            if (!backup.Success && !backup.Skipped)
            {
                result.Success = false;
                result.ErrorMessage = $"No se pudo realizar el backup: {backup.ErrorMessage}";
                return Task.FromResult(result);
            }

            var settings = LoadSettings(configFile);
            ApplySteamVrSettings(settings, profileSettings.SteamVr);
            SaveSettings(configFile, settings);

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return Task.FromResult(result);
    }

    private static Dictionary<string, object> LoadSettings(string path)
    {
        var content = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }) ?? new Dictionary<string, object>();
    }

    private static void SaveSettings(string path, Dictionary<string, object> settings)
    {
        var content = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        File.WriteAllText(path, content);
    }

    private static void ApplySteamVrSettings(Dictionary<string, object> settings, SteamVrSettings steamVr)
    {
        const string steamvrKey = "steamvr";
        if (!settings.TryGetValue(steamvrKey, out var steamvrValue) || steamvrValue is not JsonElement steamvrElement)
        {
            settings[steamvrKey] = new Dictionary<string, object>();
            steamvrElement = JsonSerializer.SerializeToElement(settings[steamvrKey]);
        }

        var steamvrDict = steamvrElement.Deserialize<Dictionary<string, object>>() ?? new Dictionary<string, object>();

        steamvrDict["supersampleScale"] = steamVr.ResolutionPerEye;
        steamvrDict["motionSmoothing"] = steamVr.MotionSmoothing;
        steamvrDict["preferredRefreshRate"] = steamVr.RefreshRate;
        steamvrDict["steamvrHome"] = false;

        settings[steamvrKey] = steamvrDict;
    }
}
