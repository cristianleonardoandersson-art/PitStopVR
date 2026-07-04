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
        var result = new ApplyResult
        {
            ComponentName = Name,
            Success = true,
            Message = $"[SIMULACION] Se aplicaria la configuracion de {Name}",
            BackupPath = $"[SIMULACION] Backup de {Name}"
        };

        return Task.FromResult(result);
    }
}
