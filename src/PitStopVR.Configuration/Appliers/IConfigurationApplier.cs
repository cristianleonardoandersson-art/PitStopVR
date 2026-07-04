using PitStopVR.Core.Models;
using PitStopVR.Knowledge.Models;

namespace PitStopVR.Configuration.Appliers;

public interface IConfigurationApplier
{
    string Name { get; }
    bool CanApply(MachineProfile profile);
    Task<ApplyResult> ApplyAsync(MachineProfile profile, Profile profileSettings);
}
