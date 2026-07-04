using PitStopVR.Core.Models;
using PitStopVR.Core.Serialization;
using PitStopVR.Knowledge;
using PitStopVR.Knowledge.Models;
using System.Diagnostics;

namespace PitStopVR.Configuration;

public sealed class ProfileApplier
{
    private readonly string _appDataPath;

    public ProfileApplier(string appDataPath)
    {
        _appDataPath = appDataPath;
    }

    public async Task<ApplyResult> ApplyAsync(MachineProfile machine, Profile profile, GameInfo game)
    {
        var result = new ApplyResult { Success = true };

        try
        {
            var backupPath = CreateBackupFolder();
            result.BackupPath = backupPath;

            if (machine.Software.SteamVrInstalled)
            {
                await ApplySteamVrAsync(profile.SteamVr, backupPath);
            }

            await LaunchGameAsync(game, profile.Game.LaunchArgs);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private string CreateBackupFolder()
    {
        var backupPath = Path.Combine(_appDataPath, "backups", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(backupPath);
        return backupPath;
    }

    private Task ApplySteamVrAsync(SteamVrSettings settings, string backupPath)
    {
        // TODO: implementar modificación de steamvr.vrsettings con backup.
        return Task.CompletedTask;
    }

    private Task LaunchGameAsync(GameInfo game, string launchArgs)
    {
        var url = $"steam://rungameid/{game.Id}//{launchArgs}";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        return Task.CompletedTask;
    }
}

public sealed class ApplyResult
{
    public bool Success { get; set; }
    public string? BackupPath { get; set; }
    public string? ErrorMessage { get; set; }
}
