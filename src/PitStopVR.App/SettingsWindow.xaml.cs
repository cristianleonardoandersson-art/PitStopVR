using PitStopVR.Telemetry.Configuration;
using System.IO;
using System.Windows;

namespace PitStopVR.App;

public partial class SettingsWindow : Window
{
    private readonly TelemetrySettingsStore _settingsStore;

    public SettingsWindow(string appDataPath)
    {
        InitializeComponent();
        _settingsStore = new TelemetrySettingsStore(appDataPath);
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _settingsStore.Load();
        AdbPathTextBox.Text = settings.AdbPath ?? string.Empty;
        SteamVRPathTextBox.Text = settings.SteamVRPath ?? string.Empty;
        MinSessionSecondsTextBox.Text = settings.MinSessionSeconds.ToString();
        PreferOpenVRCheckBox.IsChecked = settings.PreferOpenVR;
        PreferOVRMetricsToolCheckBox.IsChecked = settings.PreferOVRMetricsTool;
    }

    private void BrowseAdbButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            FileName = "adb",
            DefaultExt = ".exe",
            Filter = "ADB ejecutable (adb.exe)|adb.exe|Todos los archivos (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            AdbPathTextBox.Text = dialog.FileName;
        }
    }

    private void BrowseSteamVRButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            FileName = "openvr_api",
            DefaultExt = ".dll",
            Filter = "OpenVR API (openvr_api.dll)|openvr_api.dll|Todos los archivos (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            SteamVRPathTextBox.Text = dialog.FileName;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(MinSessionSecondsTextBox.Text, out var minSeconds) || minSeconds < 1)
        {
            MessageBox.Show("La duración mínima de sesión debe ser un número mayor a 0.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var settings = new TelemetrySettings
        {
            AdbPath = string.IsNullOrWhiteSpace(AdbPathTextBox.Text) ? null : AdbPathTextBox.Text.Trim(),
            SteamVRPath = string.IsNullOrWhiteSpace(SteamVRPathTextBox.Text) ? null : SteamVRPathTextBox.Text.Trim(),
            MinSessionSeconds = minSeconds,
            PreferOpenVR = PreferOpenVRCheckBox.IsChecked == true,
            PreferOVRMetricsTool = PreferOVRMetricsToolCheckBox.IsChecked == true
        };

        _settingsStore.Save(settings);
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
