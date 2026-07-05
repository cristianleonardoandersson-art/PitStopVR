using PitStopVR.Configuration.Appliers;
using PitStopVR.Configuration.Backup;
using PitStopVR.Core.Models;
using PitStopVR.Knowledge.Models;
using System.Diagnostics;

namespace PitStopVR.Configuration;

public sealed class ProfileApplier
{
    private readonly string _appDataPath;
    private readonly List<IConfigurationApplier>? _fixedAppliers;

    public ProfileApplier(string appDataPath)
    {
        _appDataPath = appDataPath;
        _fixedAppliers = null;
    }

    public ProfileApplier(string appDataPath, IEnumerable<IConfigurationApplier> appliers)
    {
        _appDataPath = appDataPath;
        _fixedAppliers = appliers.ToList();
    }

    public async Task<ApplySummary> ApplyAsync(MachineProfile machine, Profile profile, GameInfo game)
    {
        var sessionPath = CreateBackupFolder();
        var summary = new ApplySummary
        {
            BackupPath = sessionPath
        };

        var backupManager = new BackupManager(sessionPath);
        var isSimulated = _fixedAppliers is not null;
        List<IConfigurationApplier> appliers;

        if (_fixedAppliers is not null)
        {
            appliers = _fixedAppliers;
            foreach (var applier in appliers)
            {
                if (applier is ISessionAwareApplier sessionAware)
                {
                    sessionAware.SetSession(backupManager);
                }
            }
        }
        else
        {
            appliers =
            [
                new SteamVrApplier(backupManager),
                new OpenXrApplier(backupManager),
                new OdtApplier(backupManager)
            ];
        }

        foreach (var applier in appliers)
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
                SaveManifest(backupManager, sessionPath, profile, game, isSimulated);
                return summary;
            }
        }

        summary.Success = true;
        SaveManifest(backupManager, sessionPath, profile, game, isSimulated);
        LaunchGame(game, profile.Game.LaunchArgs);

        return summary;
    }

    private static void SaveManifest(BackupManager backupManager, string sessionPath, Profile profile, GameInfo game, bool isSimulated)
    {
        if (backupManager.Entries.Count == 0)
        {
            return;
        }

        var manifest = new BackupManifest
        {
            Timestamp = DateTime.Now,
            ProfileName = profile.Name,
            GameName = game.Name,
            SessionPath = sessionPath,
            IsSimulated = isSimulated,
            Entries = backupManager.Entries.ToList()
        };

        manifest.Save(sessionPath);
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
