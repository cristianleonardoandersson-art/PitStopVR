using PitStopVR.Configuration.Backup;

namespace PitStopVR.Configuration.Appliers;

public interface ISessionAwareApplier
{
    void SetSession(BackupManager backupManager);
}
