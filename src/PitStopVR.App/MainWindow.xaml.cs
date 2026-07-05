using PitStopVR.Configuration;
using PitStopVR.Configuration.Simulation;
using PitStopVR.Core.Models;
using PitStopVR.Inspector;
using PitStopVR.Knowledge;
using PitStopVR.Knowledge.Models;
using PitStopVR.Telemetry.Analysis;
using PitStopVR.Telemetry.Collectors;
using PitStopVR.Telemetry.Collectors.Adapters.OpenVR;
using PitStopVR.Telemetry.Configuration;
using PitStopVR.Telemetry.History;
using PitStopVR.Telemetry.Models;
using System.IO;
using System.Linq;
using System.Windows;

namespace PitStopVR.App;

public partial class MainWindow : Window
{
    private static string KnowledgePath => ResolveKnowledgePath();
    private static string AppDataPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PitStopVR");

    private MachineProfile? _currentProfile;
    private ProfileSet? _profileSet;
    private SessionRecorder? _sessionRecorder;

    public MainWindow()
    {
        InitializeComponent();
        LoadProfiles();
    }

    private void LoadProfiles()
    {
        try
        {
            var loader = new ProfileLoader(KnowledgePath);
            _profileSet = loader.LoadDefaultProfiles();
            ProfileComboBox.ItemsSource = _profileSet.Profiles;
            ProfileComboBox.DisplayMemberPath = "Name";
            ProfileComboBox.SelectedIndex = 1; // Equilibrado
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudieron cargar los perfiles: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InspectButton_Click(object sender, RoutedEventArgs e)
    {
        var inspector = new MachineInspector(KnowledgePath);
        _currentProfile = inspector.Inspect();
        ShowProfile(_currentProfile, "Escaneo real");
    }

    private void SimulateButton_Click(object sender, RoutedEventArgs e)
    {
        SimulationCheckBox.IsChecked = true;
        var inspector = new SimulatedMachineInspector();
        _currentProfile = inspector.Generate();
        ShowProfile(_currentProfile, "Simulación");
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentProfile is null)
        {
            MessageBox.Show("Primero escanea la PC o carga la simulación.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (GamesDataGrid.SelectedItem is not GameInfo selectedGame)
        {
            MessageBox.Show("Selecciona un juego de la lista.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (ProfileComboBox.SelectedItem is not Profile selectedProfile)
        {
            MessageBox.Show("Selecciona un perfil.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var isSimulation = SimulationCheckBox.IsChecked == true;
        ProfileApplier applier;

        if (isSimulation)
        {
            applier = new ProfileApplier(AppDataPath,
            [
                new SimulatedConfigurationApplier("SteamVR"),
                new SimulatedConfigurationApplier("OpenXR"),
                new SimulatedConfigurationApplier("Oculus Debug Tool")
            ]);
        }
        else
        {
            applier = new ProfileApplier(AppDataPath);
        }

        try
        {
            var result = await applier.ApplyAsync(_currentProfile, selectedProfile, selectedGame);
            var details = string.Join("\n", result.Results.Select(r =>
                $"- {r.ComponentName}: {(r.Success ? (r.Skipped ? "Omitido" : "OK") : "Fallo")} {r.Message}"));

            MessageBox.Show(
                $"Resultado: {(result.Success ? "Exitoso" : "Fallido")}\n\n" +
                $"Backup: {result.BackupPath}\n\n" +
                $"Detalles:\n{details}",
                "Aplicar perfil",
                MessageBoxButton.OK,
                result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

            if (result.Success)
            {
                await StartTelemetrySessionAsync(selectedGame, selectedProfile, _currentProfile, isSimulation);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al aplicar el perfil: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task StartTelemetrySessionAsync(GameInfo game, Profile profile, MachineProfile machine, bool isSimulation)
    {
        var settings = new TelemetrySettingsStore(AppDataPath).Load();
        OpenVRNative.ManualApiPath = settings.SteamVRPath;

        _sessionRecorder?.Dispose();
        _sessionRecorder = new SessionRecorder(AppDataPath, isSimulation, settings.AdbPath);
        await _sessionRecorder.StartSessionAsync(game, profile, machine);

        if (isSimulation)
        {
            var summary = await _sessionRecorder.StopSessionAsync();
            if (summary is not null)
            {
                summary.Bottleneck = BottleneckAnalyzer.Analyze(summary);
                SaveAndShowResults(summary);
            }
        }
        else
        {
            ApplyButton.Visibility = Visibility.Collapsed;
            EndSessionButton.Visibility = Visibility.Visible;

            MessageBox.Show(
                "Sesión de análisis iniciada. Jugá lo que quieras y, cuando termines, hacé clic en 'Finalizar sesión de análisis' para ver los resultados.",
                "Análisis de rendimiento",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private async void EndSessionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_sessionRecorder is null || !_sessionRecorder.IsRecording)
        {
            return;
        }

        try
        {
            var summary = await _sessionRecorder.StopSessionAsync();
            _sessionRecorder.Dispose();
            _sessionRecorder = null;

            ApplyButton.Visibility = Visibility.Visible;
            EndSessionButton.Visibility = Visibility.Collapsed;

            if (summary is null)
            {
                MessageBox.Show("No se pudo generar el resumen de la sesión.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveAndShowResults(summary);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al finalizar la sesión: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveAndShowResults(SessionSummary summary)
    {
        var historyStore = new SessionHistoryStore(AppDataPath);
        historyStore.SaveSession(summary);

        var resultsWindow = new SessionResultsWindow(summary) { Owner = this };
        resultsWindow.ShowDialog();
    }

    private void RestoreBackupsButton_Click(object sender, RoutedEventArgs e)
    {
        var restoreWindow = new RestoreWindow { Owner = this };
        restoreWindow.ShowDialog();
    }

    private void HistoryButton_Click(object sender, RoutedEventArgs e)
    {
        var historyWindow = new SessionHistoryWindow { Owner = this };
        historyWindow.ShowDialog();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(AppDataPath) { Owner = this };
        settingsWindow.ShowDialog();
    }

    private void EcosystemButton_Click(object sender, RoutedEventArgs e)
    {
        var machine = _currentProfile ?? new MachineProfile();
        var ecosystemWindow = new EcosystemCheckWindow(AppDataPath, machine) { Owner = this };
        ecosystemWindow.ShowDialog();
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
