using PitStopVR.Configuration.Backup;
using PitStopVR.Configuration.Restore;
using System.IO;
using System.Linq;
using System.Windows;

namespace PitStopVR.App;

public partial class RestoreWindow : Window
{
    private static string AppDataPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PitStopVR");

    private readonly RestoreManager _restoreManager;

    public RestoreWindow()
    {
        InitializeComponent();
        _restoreManager = new RestoreManager(AppDataPath);
        LoadSessions();
    }

    private void LoadSessions()
    {
        var sessions = _restoreManager.ListSessions();
        SessionsDataGrid.ItemsSource = sessions;
        RestoreButton.IsEnabled = false;
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadSessions();
    }

    private void SessionsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        RestoreButton.IsEnabled = SessionsDataGrid.SelectedItem is BackupManifest;
    }

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (SessionsDataGrid.SelectedItem is not BackupManifest manifest)
        {
            MessageBox.Show("Selecciona una sesion de backup.", "Atencion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Se restauraran {manifest.Entries.Count} componente(s) de la sesion del {manifest.Timestamp}.\n\n¿Continuar?",
            "Confirmar restauracion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        var result = _restoreManager.RestoreSession(manifest);

        var details = result.RestoredComponents.Count > 0
            ? string.Join("\n", result.RestoredComponents.Select(c => $"- {c}: restaurado"))
            : "Ningun componente restaurado.";

        if (result.Errors.Count > 0)
        {
            details += "\n" + string.Join("\n", result.Errors.Select(err => $"- ERROR: {err}"));
        }

        MessageBox.Show(
            $"Resultado: {(result.Success ? "Exitoso" : "Con errores")}\n\nDetalles:\n{details}",
            "Restaurar backup",
            MessageBoxButton.OK,
            result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
    }
}
