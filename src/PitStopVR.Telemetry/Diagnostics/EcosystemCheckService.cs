using PitStopVR.Core.Models;
using PitStopVR.Telemetry.Collectors;
using PitStopVR.Telemetry.Collectors.Adapters;
using PitStopVR.Telemetry.Collectors.Adapters.OpenVR;
using PitStopVR.Telemetry.Configuration;
using System.ComponentModel;
using System.Diagnostics;

namespace PitStopVR.Telemetry.Diagnostics;

public sealed class EcosystemCheckService
{
    private readonly TelemetrySettings _settings;
    private readonly AdbLocator _adbLocator;
    private readonly Func<string?, OVRMetricsToolAdapter> _ovrAdapterFactory;
    private readonly Func<string?, string?> _steamVrLocator;
    private readonly Func<string?, CancellationToken, Task<bool>> _isQuestConnectedFn;

    public EcosystemCheckService(TelemetrySettings settings)
        : this(settings, new AdbLocator(), path => new OVRMetricsToolAdapter(path), OpenVRNative.FindOpenVRApiPath, IsQuestConnectedAsync)
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public EcosystemCheckService(TelemetrySettings settings, AdbLocator adbLocator, Func<string?, OVRMetricsToolAdapter> ovrAdapterFactory, Func<string?, string?>? steamVrLocator = null, Func<string?, CancellationToken, Task<bool>>? isQuestConnectedFn = null)
    {
        _settings = settings;
        _adbLocator = adbLocator;
        _ovrAdapterFactory = ovrAdapterFactory;
        _steamVrLocator = steamVrLocator ?? OpenVRNative.FindOpenVRApiPath;
        _isQuestConnectedFn = isQuestConnectedFn ?? IsQuestConnectedAsync;
    }

    public async Task<IReadOnlyList<EcosystemDependency>> CheckAllAsync(MachineProfile machine, CancellationToken cancellationToken = default)
    {
        var steamVrPath = _steamVrLocator(_settings.SteamVRPath);
        var adbPath = _settings.AdbPath ?? _adbLocator.FindAdb();
        var ovrAdapter = _ovrAdapterFactory(adbPath);

        var dependencies = new List<EcosystemDependency>
        {
            new()
            {
                Type = EcosystemDependencyType.SteamVR,
                Name = "SteamVR / OpenVR",
                Description = "Runtime necesario para capturar métricas de cualquier visor compatible con SteamVR (Index, Quest con Link/Air Link, etc.).",
                IsAvailable = !string.IsNullOrWhiteSpace(steamVrPath),
                Path = steamVrPath,
                DownloadUrl = "https://store.steampowered.com/app/250820/SteamVR/",
                InstallHelpText = "Instalá SteamVR desde la tienda de Steam. Si ya lo tenés, configurá la ruta manual en Configuración.",
                IsRequired = false
            },
            new()
            {
                Type = EcosystemDependencyType.ADB,
                Name = "Android Debug Bridge (adb.exe)",
                Description = "Herramienta necesaria para conectar con Meta Quest y descargar métricas de OVR Metrics Tool.",
                IsAvailable = !string.IsNullOrWhiteSpace(adbPath),
                Path = adbPath,
                DownloadUrl = "https://developer.android.com/studio/releases/platform-tools",
                InstallHelpText = "Descargá Platform Tools y descomprimílo, o instalá SideQuest / Oculus Developer Hub. Luego configurá la ruta en Configuración.",
                IsRequired = false
            }
        };

        if (!string.IsNullOrWhiteSpace(adbPath))
        {
            dependencies.Add(new EcosystemDependency
            {
                Type = EcosystemDependencyType.OVRMetricsTool,
                Name = "OVR Metrics Tool",
                Description = "Aplicación que debe estar instalada en el Meta Quest para exponer métricas de FPS, CPU, GPU, stale frames y bitrate.",
                IsAvailable = ovrAdapter.IsAvailable(machine),
                DownloadUrl = "https://developer.oculus.com/downloads/package/ovr-metrics-tool/",
                InstallHelpText = "Instalá OVR Metrics Tool desde el Oculus Developer Center y ejecutala en el visor al menos una vez.",
                IsRequired = false
            });

            dependencies.Add(new EcosystemDependency
            {
                Type = EcosystemDependencyType.QuestConnected,
                Name = "Meta Quest conectado",
                Description = "Un Meta Quest conectado por USB o Wi-Fi con depuración ADB habilitada.",
                IsAvailable = await _isQuestConnectedFn(adbPath, cancellationToken),
                DownloadUrl = "https://developer.oculus.com/documentation/native/android/mobile-device-setup/",
                InstallHelpText = "Conectá el Quest, habilitá depuración USB en el visor y aceptá la ventana de permisos.",
                IsRequired = false
            });
        }

        return dependencies;
    }

    private static async Task<bool> IsQuestConnectedAsync(string? adbPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(adbPath))
        {
            return false;
        }

        try
        {
            var output = await RunAdbAsync(adbPath, "devices -l", cancellationToken);
            return output.Split('\n')
                .Select(line => line.Trim())
                .Any(line => line.EndsWith("device", StringComparison.OrdinalIgnoreCase) && line.Contains(' '));
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string> RunAdbAsync(string adbPath, string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(adbPath, arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("No se pudo iniciar adb.exe.");
        await process.WaitForExitAsync(cancellationToken);
        return await process.StandardOutput.ReadToEndAsync(cancellationToken);
    }
}
