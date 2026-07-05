using PitStopVR.Telemetry.Models;

namespace PitStopVR.Telemetry.Analysis;

public static class BottleneckAnalyzer
{
    public static BottleneckAnalysis Analyze(SessionSummary summary)
    {
        var bitrateRatio = summary.ConfiguredBitrateMbps > 0
            ? summary.AvgBitrateMbps / summary.ConfiguredBitrateMbps
            : 1.0;

        if (bitrateRatio < 0.65 || summary.AvgWifiSignalPercent < 45)
        {
            return new BottleneckAnalysis
            {
                Type = BottleneckType.WeakWifiSignal,
                Title = "Señal Wi-Fi débil",
                Recommendation = $"La señal Wi-Fi promedio fue {summary.AvgWifiSignalPercent:0}% y el bitrate real cayó a {summary.AvgBitrateMbps:0} Mbps (configurado: {summary.ConfiguredBitrateMbps:0} Mbps). Acercate al router, usá banda de 5GHz/6GHz dedicada o considerá un cable Link."
            };
        }

        if (summary.ReprojectedFramesPercent >= 10
            && summary.AvgGpuUsagePercent < 90
            && summary.AvgCpuUsagePercent < 90)
        {
            return new BottleneckAnalysis
            {
                Type = BottleneckType.Wireless,
                Title = "Cuello de botella inalámbrico",
                Recommendation = $"{summary.ReprojectedFramesPercent:0}% de los frames fueron reproyectados sin que la CPU/GPU estén saturadas. Bajá el bitrate del Oculus Debug Tool o probá con cable Link para descartar el enlace inalámbrico."
            };
        }

        if (summary.AvgGpuUsagePercent >= 90 && summary.AvgCpuUsagePercent <= 70)
        {
            return new BottleneckAnalysis
            {
                Type = BottleneckType.Gpu,
                Title = "Cuello de botella en GPU",
                Recommendation = $"La GPU promedió {summary.AvgGpuUsagePercent:0}% de uso (pico {summary.MaxGpuUsagePercent:0}%) mientras la CPU tuvo margen ({summary.AvgCpuUsagePercent:0}%). Bajá el supersampling/resolución por ojo en SteamVR u OpenXR."
            };
        }

        if (summary.AvgCpuUsagePercent >= 90 && summary.AvgGpuUsagePercent <= 80)
        {
            return new BottleneckAnalysis
            {
                Type = BottleneckType.Cpu,
                Title = "Cuello de botella en CPU",
                Recommendation = $"La CPU promedió {summary.AvgCpuUsagePercent:0}% de uso (pico {summary.MaxCpuUsagePercent:0}%) mientras la GPU tuvo margen ({summary.AvgGpuUsagePercent:0}%). Probá bajar la tasa de refresco o cerrar procesos en segundo plano."
            };
        }

        return new BottleneckAnalysis
        {
            Type = BottleneckType.None,
            Title = "Configuración balanceada",
            Recommendation = $"CPU ({summary.AvgCpuUsagePercent:0}%) y GPU ({summary.AvgGpuUsagePercent:0}%) están en rangos saludables, con solo {summary.ReprojectedFramesPercent:0}% de frames reproyectados. No se detectó un cuello de botella claro."
        };
    }
}
