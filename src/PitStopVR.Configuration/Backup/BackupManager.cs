using Microsoft.Win32;

namespace PitStopVR.Configuration.Backup;

public sealed class BackupManager
{
    private readonly string _sessionPath;
    private readonly List<BackupEntry> _entries = new();

    public IReadOnlyList<BackupEntry> Entries => _entries;

    public BackupManager(string sessionPath)
    {
        _sessionPath = sessionPath;
        Directory.CreateDirectory(_sessionPath);
    }

    public BackupResult BackupFile(string filePath, string componentName)
    {
        if (!File.Exists(filePath))
        {
            return BackupResult.ForSkipped(filePath, "El archivo no existe");
        }

        var fileName = Path.GetFileName(filePath);
        var backupPath = Path.Combine(_sessionPath, fileName);

        try
        {
            File.Copy(filePath, backupPath, overwrite: true);
            _entries.Add(new BackupEntry
            {
                ComponentName = componentName,
                Type = BackupEntryType.File,
                OriginalPath = filePath,
                BackupPath = backupPath
            });
            return BackupResult.Successful(filePath, backupPath);
        }
        catch (Exception ex)
        {
            return BackupResult.Failed(filePath, ex.Message);
        }
    }

    public BackupResult BackupRegistryKey(RegistryKey baseKey, string keyPath, string componentName)
    {
        var fullKeyPath = $@"{baseKey.Name}\{keyPath}";
        var fileBaseName = baseKey.Name.Contains('\\') ? baseKey.Name[(baseKey.Name.IndexOf('\\') + 1)..] : baseKey.Name;
        var backupPath = Path.Combine(_sessionPath, $"{fileBaseName}_{keyPath.Replace('\\', '_')}.reg");

        try
        {
            ExportRegistryKey(fullKeyPath, backupPath);
            _entries.Add(new BackupEntry
            {
                ComponentName = componentName,
                Type = BackupEntryType.Registry,
                OriginalPath = fullKeyPath,
                BackupPath = backupPath
            });
            return BackupResult.Successful(fullKeyPath, backupPath);
        }
        catch (Exception ex)
        {
            return BackupResult.Failed(fullKeyPath, ex.Message);
        }
    }

    public BackupResult RecordSimulatedBackup(string componentName, string description)
    {
        var fileName = $"{componentName.Replace(' ', '_')}_simulado.txt";
        var backupPath = Path.Combine(_sessionPath, fileName);
        var originalPath = $"[SIMULACION] {componentName}";

        try
        {
            File.WriteAllText(backupPath, $"[SIMULACION] {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nComponente: {componentName}\n{description}");
            _entries.Add(new BackupEntry
            {
                ComponentName = componentName,
                Type = BackupEntryType.Simulated,
                OriginalPath = originalPath,
                BackupPath = backupPath
            });
            return BackupResult.Successful(originalPath, backupPath);
        }
        catch (Exception ex)
        {
            return BackupResult.Failed(originalPath, ex.Message);
        }
    }

    private static void ExportRegistryKey(string fullKeyPath, string exportPath)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "reg.exe",
            Arguments = $"export \"{fullKeyPath}\" \"{exportPath}\" /y",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo)!;
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"No se pudo exportar la clave de registro {fullKeyPath}: {error}");
        }
    }
}

public sealed class BackupResult
{
    public string OriginalPath { get; }
    public string? BackupPath { get; }
    public bool Success { get; }
    public bool Skipped { get; }
    public string? ErrorMessage { get; }

    private BackupResult(string originalPath, string? backupPath, bool success, bool skipped, string? errorMessage)
    {
        OriginalPath = originalPath;
        BackupPath = backupPath;
        Success = success;
        Skipped = skipped;
        ErrorMessage = errorMessage;
    }

    public static BackupResult Successful(string originalPath, string backupPath)
        => new(originalPath, backupPath, true, false, null);

    public static BackupResult ForSkipped(string originalPath, string reason)
        => new(originalPath, null, true, true, reason);

    public static BackupResult Failed(string originalPath, string error)
        => new(originalPath, null, false, false, error);
}
