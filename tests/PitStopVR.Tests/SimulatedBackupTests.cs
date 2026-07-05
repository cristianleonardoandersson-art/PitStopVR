using PitStopVR.Configuration.Appliers;
using PitStopVR.Configuration.Backup;
using PitStopVR.Configuration.Restore;
using PitStopVR.Configuration.Simulation;
using PitStopVR.Knowledge.Models;

namespace PitStopVR.Tests;

public class SimulatedBackupTests : IDisposable
{
    private readonly string _tempSessionPath;

    public SimulatedBackupTests()
    {
        _tempSessionPath = Path.Combine(Path.GetTempPath(), "PitStopVRTests_" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempSessionPath))
        {
            Directory.Delete(_tempSessionPath, recursive: true);
        }
    }

    [Fact]
    public async Task SimulatedApplier_RecordsBackupEntry_WhenSessionIsSet()
    {
        var backupManager = new BackupManager(_tempSessionPath);
        var applier = new SimulatedConfigurationApplier("SteamVR");
        ((ISessionAwareApplier)applier).SetSession(backupManager);

        var profile = new PitStopVR.Core.Models.MachineProfile();
        var settings = new Profile
        {
            Name = "Sim",
            SteamVr = new SteamVrSettings { ResolutionPerEye = 1.2, MotionSmoothing = true, RefreshRate = 90 }
        };

        var result = await applier.ApplyAsync(profile, settings);

        Assert.True(result.Success);
        Assert.Single(backupManager.Entries);
        Assert.Equal(BackupEntryType.Simulated, backupManager.Entries[0].Type);
        Assert.True(File.Exists(backupManager.Entries[0].BackupPath));
    }

    [Fact]
    public void BackupManifest_SaveAndLoad_RoundTrips()
    {
        Directory.CreateDirectory(_tempSessionPath);
        var manifest = new BackupManifest
        {
            Timestamp = DateTime.Now,
            ProfileName = "Perfil Test",
            GameName = "Juego Test",
            SessionPath = _tempSessionPath,
            IsSimulated = true,
            Entries = new List<BackupEntry>
            {
                new()
                {
                    ComponentName = "SteamVR",
                    Type = BackupEntryType.Simulated,
                    OriginalPath = "[SIMULACION] SteamVR",
                    BackupPath = Path.Combine(_tempSessionPath, "SteamVR_simulado.txt")
                }
            }
        };

        manifest.Save(_tempSessionPath);

        var manifestPath = Path.Combine(_tempSessionPath, BackupManifest.FileName);
        Assert.True(File.Exists(manifestPath));

        var loaded = BackupManifest.Load(manifestPath);

        Assert.NotNull(loaded);
        Assert.Equal("Perfil Test", loaded!.ProfileName);
        Assert.True(loaded.IsSimulated);
        Assert.Single(loaded.Entries);
    }

    [Fact]
    public void RestoreManager_ListSessions_ReadsManifestsFromBackupsFolder()
    {
        var appDataPath = Path.Combine(Path.GetTempPath(), "PitStopVRTests_AppData_" + Guid.NewGuid().ToString("N"));
        var sessionPath = Path.Combine(appDataPath, "backups", "20260704_120000");
        Directory.CreateDirectory(sessionPath);

        var manifest = new BackupManifest
        {
            Timestamp = DateTime.Now,
            ProfileName = "Perfil Test",
            GameName = "Juego Test",
            SessionPath = sessionPath,
            IsSimulated = true,
            Entries = new List<BackupEntry>
            {
                new()
                {
                    ComponentName = "OpenXR",
                    Type = BackupEntryType.Simulated,
                    OriginalPath = "[SIMULACION] OpenXR",
                    BackupPath = Path.Combine(sessionPath, "OpenXR_simulado.txt")
                }
            }
        };
        manifest.Save(sessionPath);

        try
        {
            var restoreManager = new RestoreManager(appDataPath);
            var sessions = restoreManager.ListSessions();

            Assert.Single(sessions);
            Assert.Equal("Perfil Test", sessions[0].ProfileName);

            var restoreResult = restoreManager.RestoreSession(sessions[0]);
            Assert.True(restoreResult.Success);
            Assert.Single(restoreResult.RestoredComponents);
            Assert.Contains("simulado", restoreResult.RestoredComponents[0]);
        }
        finally
        {
            Directory.Delete(appDataPath, recursive: true);
        }
    }
}
