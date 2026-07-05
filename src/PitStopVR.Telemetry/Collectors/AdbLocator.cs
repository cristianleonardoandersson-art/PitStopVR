namespace PitStopVR.Telemetry.Collectors;

public class AdbLocator
{
    public virtual string? FindAdb()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        var candidatePaths = new List<string?>
        {
            GetEnvironmentPath("ANDROID_HOME", "platform-tools", "adb.exe"),
            GetEnvironmentPath("ANDROID_SDK", "platform-tools", "adb.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk", "platform-tools", "adb.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "android-sdk", "platform-tools", "adb.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "android-sdk", "platform-tools", "adb.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "sidequest", "sidequest", "resources", "app.asar.unpacked", "build", "platform-tools", "adb.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Oculus", "Developer Hub", "resources", "Support", "platform-tools", "adb.exe"),
        };

        foreach (var path in candidatePaths)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                return path;
            }
        }

        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrWhiteSpace(pathVariable))
        {
            foreach (var pathDir in pathVariable.Split(Path.PathSeparator))
            {
                var adbPath = Path.Combine(pathDir.Trim('"'), "adb.exe");
                if (File.Exists(adbPath))
                {
                    return adbPath;
                }
            }
        }

        return null;
    }

    private static string? GetEnvironmentPath(string variable, string relativePath, string fileName)
    {
        var root = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(root))
        {
            return null;
        }

        return Path.Combine(root, relativePath, fileName);
    }
}
