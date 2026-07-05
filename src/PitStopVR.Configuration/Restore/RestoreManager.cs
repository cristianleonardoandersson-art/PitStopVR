using PitStopVR.Configuration.Backup;
using System.Diagnostics;

namespace PitStopVR.Configuration.Restore;

public sealed class RestoreManager
{
    private readonly string _backupsRoot;

    public RestoreManager(string appDataPath)
    {
        _backupsRoot = Path.Combine(appDataPath, "backups");
    }

    public List<BackupManifest> ListSessions()
    {
        var sessions = new List<BackupManifest>();

        if (!Directory.Exists(_backupsRoot))
        {
            return sessions;
        }

        foreach (var dir in Directory.GetDirectories(_backupsRoot))
        {
            var manifestPath = Path.Combine(dir, BackupManifest.FileName);

            try
            {
                var manifest = BackupManifest.Load(manifestPath);
                if (manifest is not null)
                {
                    sessions.Add(manifest);
                }
            }
            catch
            {
                // Manifiesto corrupto o ilegible: se omite de la lista.
            }
        }

        return sessions.OrderByDescending(s => s.Timestamp).ToList();
    }

    public RestoreResult RestoreSession(BackupManifest manifest)
    {
        var result = new RestoreResult { SessionPath = manifest.SessionPath };

        foreach (var entry in manifest.Entries)
        {
            try
            {
                switch (entry.Type)
                {
                    case BackupEntryType.File:
                        RestoreFile(entry);
                        result.RestoredComponents.Add(entry.ComponentName);
                        break;
                    case BackupEntryType.Registry:
                        RestoreRegistryKey(entry);
                        result.RestoredComponents.Add(entry.ComponentName);
                        break;
                    default:
                        result.RestoredComponents.Add($"{entry.ComponentName} (simulado, sin cambios reales)");
                        break;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{entry.ComponentName}: {ex.Message}");
            }
        }

        result.Success = result.Errors.Count == 0;
        return result;
    }

    private static void RestoreFile(BackupEntry entry)
    {
        if (!File.Exists(entry.BackupPath))
        {
            throw new FileNotFoundException($"No se encontro el archivo de backup: {entry.BackupPath}");
        }

        var targetDir = Path.GetDirectoryName(entry.OriginalPath);
        if (!string.IsNullOrWhiteSpace(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        File.Copy(entry.BackupPath, entry.OriginalPath, overwrite: true);
    }

    private static void RestoreRegistryKey(BackupEntry entry)
    {
        if (!File.Exists(entry.BackupPath))
        {
            throw new FileNotFoundException($"No se encontro el archivo .reg de backup: {entry.BackupPath}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "reg.exe",
            Arguments = $"import \"{entry.BackupPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)!;
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"No se pudo importar el registro: {error}");
        }
    }
}

public sealed class RestoreResult
{
    public bool Success { get; set; }
    public string? SessionPath { get; set; }
    public List<string> RestoredComponents { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
