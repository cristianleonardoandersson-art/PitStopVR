using PitStopVR.Telemetry.Models;

namespace PitStopVR.Telemetry.History;

public sealed class SessionHistoryStore
{
    private readonly string _historyRoot;

    public SessionHistoryStore(string appDataPath)
    {
        _historyRoot = Path.Combine(appDataPath, "history");
    }

    public string SaveSession(SessionSummary summary)
    {
        var safeGameId = string.IsNullOrWhiteSpace(summary.GameId)
            ? "sin-juego"
            : SanitizeForPath(summary.GameId);

        var gameFolder = Path.Combine(_historyRoot, safeGameId);
        var filePath = Path.Combine(gameFolder, $"{summary.FileNameTimestamp}{SessionSummary.FileExtension}");

        summary.Save(filePath);
        return filePath;
    }

    public List<SessionSummary> ListSessions(string? gameId = null)
    {
        var sessions = new List<SessionSummary>();

        if (!Directory.Exists(_historyRoot))
        {
            return sessions;
        }

        var searchFolders = string.IsNullOrWhiteSpace(gameId)
            ? Directory.GetDirectories(_historyRoot)
            : new[] { Path.Combine(_historyRoot, SanitizeForPath(gameId)) };

        foreach (var folder in searchFolders)
        {
            if (!Directory.Exists(folder))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(folder, $"*{SessionSummary.FileExtension}"))
            {
                try
                {
                    var summary = SessionSummary.Load(file);
                    if (summary is not null)
                    {
                        sessions.Add(summary);
                    }
                }
                catch
                {
                    // Archivo de historial corrupto o ilegible: se omite.
                }
            }
        }

        return sessions.OrderByDescending(s => s.StartedAt).ToList();
    }

    public bool DeleteSession(SessionSummary summary)
    {
        if (string.IsNullOrWhiteSpace(summary.GameId))
        {
            return false;
        }

        var gameFolder = Path.Combine(_historyRoot, SanitizeForPath(summary.GameId));
        var filePath = Path.Combine(gameFolder, $"{summary.FileNameTimestamp}{SessionSummary.FileExtension}");

        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            File.Delete(filePath);

            if (Directory.Exists(gameFolder) && !Directory.EnumerateFileSystemEntries(gameFolder).Any())
            {
                Directory.Delete(gameFolder);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string SanitizeForPath(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "sin-juego" : sanitized;
    }
}
