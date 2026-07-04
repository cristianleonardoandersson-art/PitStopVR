using Microsoft.Win32;
using System.Text;

namespace PitStopVR.Configuration.Backup;

public sealed class BackupManager
{
    private readonly string _backupBasePath;

    public BackupManager(string backupBasePath)
    {
        _backupBasePath = backupBasePath;
    }

    public BackupResult BackupFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return BackupResult.ForSkipped(filePath, "El archivo no existe");
        }

        var backupFolder = CreateTimestampedFolder();
        var fileName = Path.GetFileName(filePath);
        var backupPath = Path.Combine(backupFolder, fileName);

        try
        {
            File.Copy(filePath, backupPath, overwrite: true);
            return BackupResult.Successful(filePath, backupPath);
        }
        catch (Exception ex)
        {
            return BackupResult.Failed(filePath, ex.Message);
        }
    }

    public BackupResult BackupRegistryKey(RegistryKey baseKey, string keyPath)
    {
        var backupFolder = CreateTimestampedFolder();
        var baseKeyName = baseKey.Name.Contains('\\') ? baseKey.Name[(baseKey.Name.IndexOf('\\') + 1)..] : baseKey.Name;
        var backupPath = Path.Combine(backupFolder, $"{baseKeyName}_{keyPath.Replace('\\', '_')}.reg");

        try
        {
            ExportRegistryKey(keyPath, backupPath);
            return BackupResult.Successful(keyPath, backupPath);
        }
        catch (Exception ex)
        {
            return BackupResult.Failed(keyPath, ex.Message);
        }
    }

    private string CreateTimestampedFolder()
    {
        var folder = Path.Combine(_backupBasePath, DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static void ExportRegistryKey(string keyPath, string exportPath)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "reg.exe",
            Arguments = $"export \"{keyPath}\" \"{exportPath}\" /y",
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
            throw new InvalidOperationException($"No se pudo exportar la clave de registro {keyPath}: {error}");
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
