using PitStopVR.Core.Models;
using PitStopVR.Inspector;
using System.IO;
using System.Windows;

namespace PitStopVR.App;

public partial class MainWindow : Window
{
    private static string KnowledgePath => ResolveKnowledgePath();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void InspectButton_Click(object sender, RoutedEventArgs e)
    {
        var inspector = new MachineInspector(KnowledgePath);
        var profile = inspector.Inspect();
        ShowProfile(profile, "Escaneo real");
    }

    private void SimulateButton_Click(object sender, RoutedEventArgs e)
    {
        var inspector = new SimulatedMachineInspector();
        var profile = inspector.Generate();
        ShowProfile(profile, "Simulación");
    }

    private static string ResolveKnowledgePath()
    {
        var baseDirectory = AppContext.BaseDirectory;

        var candidatePaths = new[]
        {
            Path.Combine(baseDirectory, "..", "..", "..", "..", "..", "knowledge"),
            Path.Combine(baseDirectory, "..", "knowledge"),
            Path.Combine(baseDirectory, "knowledge")
        };

        foreach (var path in candidatePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return string.Empty;
    }

    private void ShowProfile(MachineProfile profile, string mode)
    {
        GamesDataGrid.ItemsSource = profile.Games;

        MessageBox.Show(
            $"Modo: {mode}\n\n" +
            $"CPU: {profile.Hardware.Cpu}\n" +
            $"GPU: {profile.Hardware.Gpu}\n" +
            $"RAM: {profile.Hardware.RamBytes / 1024 / 1024 / 1024} GB\n\n" +
            $"Steam instalado: {profile.Software.SteamInstalled}\n" +
            $"SteamVR instalado: {profile.Software.SteamVrInstalled}\n" +
            $"Meta Quest Link instalado: {profile.Software.MetaQuestLinkInstalled}\n" +
            $"OpenXR runtime detectado: {profile.Software.OpenXrRuntimeDetected}\n" +
            $"Juegos detectados: {profile.Games.Count}",
            "Resultado del escaneo");
    }
}