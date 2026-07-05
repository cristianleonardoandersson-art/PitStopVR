using PitStopVR.Core.Models;
using PitStopVR.Telemetry.Models;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PitStopVR.Telemetry.Collectors.Adapters;

public class OVRMetricsToolAdapter
{
    private readonly string? _adbPath;

    public OVRMetricsToolAdapter(string? adbPath = null)
    {
        _adbPath = adbPath;
    }

    public virtual bool IsAvailable(MachineProfile machine)
    {
        return !string.IsNullOrWhiteSpace(ResolveAdbPath());
    }

    public async Task EnableCsvCaptureAsync(CancellationToken cancellationToken = default)
    {
        var adbPath = ResolveAdbPath();
        if (string.IsNullOrWhiteSpace(adbPath))
        {
            throw new InvalidOperationException("No se encontró adb.exe. Conectá el Quest o configurá la ruta manualmente.");
        }

        await RunAdbAsync(
            adbPath,
            "shell am broadcast -n com.oculus.ovrmonitormetricsservice/.SettingsBroadcastReceiver -a com.oculus.ovrmonitormetricsservice.ENABLE_CSV",
            cancellationToken);
    }

    public async Task DisableCsvCaptureAsync(CancellationToken cancellationToken = default)
    {
        var adbPath = ResolveAdbPath();
        if (string.IsNullOrWhiteSpace(adbPath))
        {
            return;
        }

        await RunAdbAsync(
            adbPath,
            "shell am broadcast -n com.oculus.ovrmonitormetricsservice/.SettingsBroadcastReceiver -a com.oculus.ovrmonitormetricsservice.DISABLE_CSV",
            cancellationToken);
    }

    public async Task<string?> PullLatestCsvAsync(string destinationDirectory, CancellationToken cancellationToken = default)
    {
        var adbPath = ResolveAdbPath();
        if (string.IsNullOrWhiteSpace(adbPath))
        {
            return null;
        }

        var remoteFolder = "/sdcard/Android/data/com.oculus.ovrmonitormetricsservice/files/CapturedMetrics/";
        var listOutput = await RunAdbAsync(adbPath, $"shell ls -t {remoteFolder}", cancellationToken);
        var latestFile = listOutput.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(latestFile))
        {
            return null;
        }

        Directory.CreateDirectory(destinationDirectory);
        var localPath = Path.Combine(destinationDirectory, SanitizeFileName(latestFile));
        await RunAdbAsync(adbPath, $"pull {remoteFolder}{latestFile} \"{localPath}\"", cancellationToken);

        return localPath;
    }

    public List<RawSessionSample> ParseCsv(string csvPath)
    {
        var samples = new List<RawSessionSample>();

        if (!File.Exists(csvPath))
        {
            return samples;
        }

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length < 2)
        {
            return samples;
        }

        var headers = lines[0].Split(',').Select(h => h.Trim('"').Trim()).ToList();

        int GetIndex(string name)
        {
            return headers.FindIndex(h => h.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        var fpsIndex = GetIndex("average_frame_rate");
        var cpuIndex = GetIndex("cpu_utilization_percentage");
        var gpuIndex = GetIndex("gpu_utilization_percentage");
        var staleIndex = GetIndex("stale_frame_count");

        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            var values = lines[i].Split(',').Select(v => v.Trim('"').Trim()).ToList();
            var sample = new RawSessionSample
            {
                Timestamp = DateTime.Now.AddSeconds(-(lines.Length - i)),
                Source = SessionSourceType.OVRMetricsTool
            };

            if (fpsIndex >= 0 && fpsIndex < values.Count && double.TryParse(values[fpsIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var fps))
            {
                sample.FpsEstimate = fps;
            }

            if (cpuIndex >= 0 && cpuIndex < values.Count && double.TryParse(values[cpuIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var cpu))
            {
                sample.CpuUsagePercent = cpu;
            }

            if (gpuIndex >= 0 && gpuIndex < values.Count && double.TryParse(values[gpuIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var gpu))
            {
                sample.GpuUsagePercent = gpu;
            }

            if (staleIndex >= 0 && staleIndex < values.Count && double.TryParse(values[staleIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var stale))
            {
                sample.ReprojectedFrames = (int)stale;
            }

            samples.Add(sample);
        }

        return samples;
    }

    private string? ResolveAdbPath()
    {
        if (!string.IsNullOrWhiteSpace(_adbPath) && File.Exists(_adbPath))
        {
            return _adbPath;
        }

        return new AdbLocator().FindAdb();
    }

    private static async Task<string> RunAdbAsync(string adbPath, string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = adbPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            throw new InvalidOperationException("No se pudo iniciar adb.exe.");
        }

        await process.WaitForExitAsync(cancellationToken);

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);

        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            throw new InvalidOperationException($"ADB error: {error}");
        }

        return output;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(sanitized) ? "ovr_metrics.csv" : sanitized;
    }
}
