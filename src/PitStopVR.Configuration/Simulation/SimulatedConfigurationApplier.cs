using PitStopVR.Configuration.Appliers;
using PitStopVR.Core.Models;
using PitStopVR.Knowledge.Models;

namespace PitStopVR.Configuration.Simulation;

public sealed class SimulatedConfigurationApplier : IConfigurationApplier
{
    public string Name { get; }

    public SimulatedConfigurationApplier(string name)
    {
        Name = name;
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

        var result = new ApplyResult
        {
            ComponentName = Name,
            Success = true,
            Message = $"[SIMULACION] Se aplicaria la configuracion de {Name}{extra}",
            BackupPath = $"[SIMULACION] Backup de {Name}"
        };

        return Task.FromResult(result);
    }
}
