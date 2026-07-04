using PitStopVR.Configuration.Appliers;
using PitStopVR.Configuration.Backup;
using PitStopVR.Core.Models;
using PitStopVR.Knowledge.Models;
using System.Diagnostics;

namespace PitStopVR.Configuration;

public sealed class ProfileApplier
{
    private readonly string _appDataPath;
    private readonly List<IConfigurationApplier> _appliers;

    public ProfileApplier(string appDataPath)
    {
        _appDataPath = appDataPath;
        var backupManager = new BackupManager(_appDataPath);
        _appliers =
        [
            new SteamVrApplier(backupManager),
            new OpenXrApplier(backupManager)
        ];
    }

    public ProfileApplier(string appDataPath, IEnumerable<IConfigurationApplier> appliers)
    {
        _appDataPath = appDataPath;
        _appliers = appliers.ToList();
    }

    public async Task<ApplySummary> ApplyAsync(MachineProfile machine, Profile profile, GameInfo game)
    {
        var summary = new ApplySummary
        {
            BackupPath = CreateBackupFolder()
        };

        foreach (var applier in _appliers)
        {
            if (!applier.CanApply(machine))
            {
                summary.Results.Add(new ApplyResult
                {
                    ComponentName = applier.Name,
                    Success = true,
                    Skipped = true,
                    Message = "Componente no disponible en esta maquina"
                });
                continue;
            }

            var result = await applier.ApplyAsync(machine, profile);
            summary.Results.Add(result);

            if (!result.Success)
            {
                summary.Success = false;
                summary.ErrorMessage = $"Fallo {applier.Name}: {result.ErrorMessage}";
                return summary;
            }
        }

        summary.Success = true;
        LaunchGame(game, profile.Game.LaunchArgs);

        return summary;
    }

    private string CreateBackupFolder()
    {
        var backupPath = Path.Combine(_appDataPath, "backups", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(backupPath);
        return backupPath;
    }

    private static void LaunchGame(GameInfo game, string launchArgs)
    {
        if (game.Source == GameSource.Steam)
        {
            var url = $"steam://rungameid/{game.Id}//{launchArgs}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        else if (File.Exists(game.ExecutablePath))
        {
            Process.Start(new ProcessStartInfo(game.ExecutablePath, launchArgs) { UseShellExecute = true });
        }
        else
        {
            throw new InvalidOperationException($"No se encontro ejecutable para lanzar {game.Name}");
        }
    }
}

public sealed class ApplySummary
{
    public bool Success { get; set; }
    public string? BackupPath { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ApplyResult> Results { get; set; } = new();
}

public sealed class ApplyResult
{
    public string ComponentName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool Skipped { get; set; }
    public string? BackupPath { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}
