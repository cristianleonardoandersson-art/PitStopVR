using PitStopVR.Configuration.Appliers;
using PitStopVR.Configuration.Backup;
using PitStopVR.Core.Models;
using PitStopVR.Knowledge.Models;

namespace PitStopVR.Configuration.Simulation;

public sealed class SimulatedConfigurationApplier : IConfigurationApplier, ISessionAwareApplier
{
    private BackupManager? _backupManager;

    public string Name { get; }

    public SimulatedConfigurationApplier(string name)
    {
        Name = name;
    }

    public void SetSession(BackupManager backupManager)
    {
        _backupManager = backupManager;
    }

    public bool CanApply(MachineProfile profile)
    {
        return true;
    }

    public Task<ApplyResult> ApplyAsync(MachineProfile profile, Profile profileSettings)
    {
        var extra = Name switch
        {
            "Oculus Debug Tool" => $" - bitrate {profileSettings.Odt.BitrateMbps} Mbps, refresh {profileSettings.Odt.RefreshRate} Hz, foveation {profileSettings.Odt.FoveationLevel}",
            "SteamVR" => $" - supersampling {profileSettings.SteamVr.ResolutionPerEye}, motion smoothing {profileSettings.SteamVr.MotionSmoothing}, refresh {profileSettings.SteamVr.RefreshRate} Hz",
            "OpenXR" => $" - runtime {profileSettings.OpenXr.PreferredRuntime}",
            _ => string.Empty
        };

        var message = $"[SIMULACION] Se aplicaria la configuracion de {Name}{extra}";
        var backupPath = $"[SIMULACION] Backup de {Name}";

        if (_backupManager is not null)
        {
            var backup = _backupManager.RecordSimulatedBackup(Name, message);
            backupPath = backup.BackupPath ?? backupPath;
        }

        var result = new ApplyResult
        {
            ComponentName = Name,
            Success = true,
            Message = message,
            BackupPath = backupPath
        };

        return Task.FromResult(result);
    }
}
