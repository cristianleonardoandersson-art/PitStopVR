using Microsoft.Win32;
using PitStopVR.Configuration.Backup;
using PitStopVR.Core.Models;
using PitStopVR.Knowledge.Models;

namespace PitStopVR.Configuration.Appliers;

public sealed class OpenXrApplier : IConfigurationApplier
{
    private readonly BackupManager _backupManager;

    public string Name => "OpenXR";

    public OpenXrApplier(BackupManager backupManager)
    {
        _backupManager = backupManager;
    }

    public bool CanApply(MachineProfile profile)
    {
        return profile.Software.OpenXrRuntimeDetected
            && !string.IsNullOrWhiteSpace(profile.Software.OpenXrRuntimePath);
    }

    public Task<ApplyResult> ApplyAsync(MachineProfile profile, Profile profileSettings)
    {
        var result = new ApplyResult { ComponentName = Name };

        try
        {
            var (baseKey, keyPath) = FindOpenXrKey();
            if (baseKey is null)
            {
                result.Success = false;
                result.ErrorMessage = "No se encontro la clave de registro de OpenXR.";
                return Task.FromResult(result);
            }

            var backup = _backupManager.BackupRegistryKey(baseKey, keyPath, Name);
            result.BackupPath = backup.BackupPath;

            if (!backup.Success && !backup.Skipped)
            {
                result.Success = false;
                result.ErrorMessage = $"No se pudo realizar el backup: {backup.ErrorMessage}";
                return Task.FromResult(result);
            }

            var runtimePath = ResolveRuntimePath(profile, profileSettings);
            if (string.IsNullOrWhiteSpace(runtimePath))
            {
                result.Success = false;
                result.ErrorMessage = "No se pudo determinar la ruta del runtime de OpenXR.";
                return Task.FromResult(result);
            }

            using var key = baseKey.OpenSubKey(keyPath, writable: true);
            if (key is null)
            {
                result.Success = false;
                result.ErrorMessage = "No se pudo abrir la clave de registro de OpenXR para escritura.";
                return Task.FromResult(result);
            }

            key.SetValue("ActiveRuntime", runtimePath, RegistryValueKind.String);

            result.Success = true;
            result.Message = $"Runtime activo establecido a: {runtimePath}";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return Task.FromResult(result);
    }

    private static (RegistryKey? BaseKey, string KeyPath) FindOpenXrKey()
    {
        const string keyPath = @"SOFTWARE\Khronos\OpenXR\1";

        using var localKey = Registry.LocalMachine.OpenSubKey(keyPath);
        if (localKey is not null)
        {
            return (Registry.LocalMachine, keyPath);
        }

        using var currentKey = Registry.CurrentUser.OpenSubKey(keyPath);
        if (currentKey is not null)
        {
            return (Registry.CurrentUser, keyPath);
        }

        return (null, keyPath);
    }

    private static string? ResolveRuntimePath(MachineProfile profile, Profile profileSettings)
    {
        var preferred = profileSettings.OpenXr.PreferredRuntime.ToLowerInvariant();

        if (preferred == "steamvr" && profile.Software.SteamVrInstalled)
        {
            return Path.Combine(profile.Software.SteamVrPath!, "steamxr_win64.json");
        }

        if (preferred == "oculus" && profile.Software.MetaQuestLinkInstalled)
        {
            return @"C:\Program Files\Oculus\Support\oculus-runtime\oculus_openxr_64.json";
        }

        return profile.Software.OpenXrRuntimePath;
    }
}
