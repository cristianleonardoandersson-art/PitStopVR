using PitStopVR.Inspector;
using System.Windows;

namespace PitStopVR.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void InspectButton_Click(object sender, RoutedEventArgs e)
    {
        var inspector = new MachineInspector();
        var profile = inspector.Inspect();
        GamesDataGrid.ItemsSource = profile.Games;

        MessageBox.Show(
            $"Steam instalado: {profile.Software.SteamInstalled}\n" +
            $"SteamVR instalado: {profile.Software.SteamVrInstalled}\n" +
            $"Meta Quest Link instalado: {profile.Software.MetaQuestLinkInstalled}\n" +
            $"Juegos detectados: {profile.Games.Count}",
            "Resultado del escaneo");
    }
}