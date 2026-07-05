using Microsoft.Win32;
using PitStopVR.Telemetry.Models;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PitStopVR.App;

public partial class SessionResultsWindow : Window
{
    private readonly SessionSummary _summary;

    public SessionResultsWindow(SessionSummary summary)
    {
        InitializeComponent();
        _summary = summary;
        PopulateHeader();
        PopulateStats();
        PopulateBottleneck();
    }

    public static bool ExportSummary(SessionSummary summary)
    {
        var dialog = new SaveFileDialog
        {
            FileName = $"PitStopVR_{summary.GameName}_{summary.FileNameTimestamp}.txt",
            Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*",
            DefaultExt = ".txt"
        };

        if (dialog.ShowDialog() != true)
        {
            return false;
        }

        var content = $@"PitStopVR - Resultado de sesión
================================
Juego: {summary.GameName}
Perfil: {summary.ProfileName}
Modo: {(summary.IsSimulated ? "Simulado" : "Real")}
Fuente de datos: {summary.DataSourceLabel}
Duración: {summary.DurationSeconds:0} segundos
Fecha: {summary.StartedAt:g}

Resumen de rendimiento
----------------------
FPS promedio: {summary.AvgFps:0.0} (mínimo: {summary.MinFps:0.0})
GPU: {summary.AvgGpuUsagePercent:0.0}% promedio, {summary.MaxGpuUsagePercent:0.0}% pico
CPU: {summary.AvgCpuUsagePercent:0.0}% promedio, {summary.MaxCpuUsagePercent:0.0}% pico
Frames dropeados: {summary.DroppedFramesPercent:0.0}%
Frames reproyectados: {summary.ReprojectedFramesPercent:0.0}%
Bitrate promedio: {summary.AvgBitrateMbps:0.0} Mbps (configurado: {summary.ConfiguredBitrateMbps:0.0} Mbps)
Señal Wi-Fi promedio: {summary.AvgWifiSignalPercent:0.0}%

Cuello de botella detectado
---------------------------
{summary.Bottleneck.Title}

Recomendación
-------------
{summary.Bottleneck.Recommendation}

Muestras incluidas: {summary.Samples.Count}
";

        try
        {
            File.WriteAllText(dialog.FileName, content);
            MessageBox.Show(
                $"Resultado exportado a:\n{dialog.FileName}",
                "Exportar",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo exportar el archivo: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }

    private void PopulateHeader()
    {
        HeaderTextBlock.Text = $"{_summary.GameName} — {_summary.ProfileName}";
        var modo = _summary.IsSimulated ? "Simulado" : "Real";
        SubHeaderTextBlock.Text = $"Modo: {modo}  |  Fuente de datos: {_summary.DataSourceLabel}  |  Duración: {_summary.DurationSeconds:0} s  |  Fecha: {_summary.StartedAt:g}";
    }

    private void PopulateStats()
    {
        StatsTextBlock.Text =
            $"FPS promedio: {_summary.AvgFps:0.0}  (mínimo: {_summary.MinFps:0.0})\n" +
            $"GPU: {_summary.AvgGpuUsagePercent:0.0}% promedio, {_summary.MaxGpuUsagePercent:0.0}% pico\n" +
            $"CPU: {_summary.AvgCpuUsagePercent:0.0}% promedio, {_summary.MaxCpuUsagePercent:0.0}% pico\n" +
            $"Frames dropeados: {_summary.DroppedFramesPercent:0.0}%\n" +
            $"Frames reproyectados: {_summary.ReprojectedFramesPercent:0.0}%\n" +
            $"Bitrate: {_summary.AvgBitrateMbps:0.0} Mbps (configurado: {_summary.ConfiguredBitrateMbps:0.0} Mbps)\n" +
            $"Señal Wi-Fi promedio: {_summary.AvgWifiSignalPercent:0.0}%";
    }

    private void PopulateBottleneck()
    {
        BottleneckTitleTextBlock.Text = _summary.Bottleneck.Title;
        BottleneckRecommendationTextBlock.Text = _summary.Bottleneck.Recommendation;

        BottleneckBorder.Background = _summary.Bottleneck.Type switch
        {
            BottleneckType.None => new SolidColorBrush(Color.FromRgb(0xE3, 0xF5, 0xE1)),
            BottleneckType.Gpu => new SolidColorBrush(Color.FromRgb(0xFD, 0xE9, 0xD9)),
            BottleneckType.Cpu => new SolidColorBrush(Color.FromRgb(0xFD, 0xE9, 0xD9)),
            BottleneckType.Wireless => new SolidColorBrush(Color.FromRgb(0xFC, 0xE4, 0xE4)),
            BottleneckType.WeakWifiSignal => new SolidColorBrush(Color.FromRgb(0xFC, 0xE4, 0xE4)),
            _ => new SolidColorBrush(Color.FromRgb(0xEF, 0xEF, 0xEF))
        };
    }

    private void FpsChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        DrawFpsChart();
    }

    private void DrawFpsChart()
    {
        FpsChartCanvas.Children.Clear();

        if (_summary.Samples.Count < 2)
        {
            return;
        }

        var width = FpsChartCanvas.ActualWidth;
        var height = FpsChartCanvas.ActualHeight;

        if (width <= 0 || height <= 0)
        {
            return;
        }

        var maxFps = Math.Max(1, _summary.Samples.Max(s => s.FpsEstimate));
        var minFps = Math.Min(maxFps, _summary.Samples.Min(s => s.FpsEstimate));
        var range = Math.Max(1, maxFps - minFps);

        var points = new PointCollection();
        var stepX = width / (_summary.Samples.Count - 1);

        for (var i = 0; i < _summary.Samples.Count; i++)
        {
            var normalized = (_summary.Samples[i].FpsEstimate - minFps) / range;
            var x = i * stepX;
            var y = height - normalized * (height - 10) - 5;
            points.Add(new System.Windows.Point(x, y));
        }

        var polyline = new Polyline
        {
            Points = points,
            Stroke = Brushes.SteelBlue,
            StrokeThickness = 2
        };

        FpsChartCanvas.Children.Add(polyline);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        ExportSummary(_summary);
    }
}
