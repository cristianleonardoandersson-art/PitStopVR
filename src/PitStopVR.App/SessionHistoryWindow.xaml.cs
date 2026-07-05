using PitStopVR.Telemetry.History;
using PitStopVR.Telemetry.Models;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace PitStopVR.App;

public partial class SessionHistoryWindow : Window
{
    private static string AppDataPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PitStopVR");

    private readonly SessionHistoryStore _historyStore;

    public SessionHistoryWindow()
    {
        InitializeComponent();
        _historyStore = new SessionHistoryStore(AppDataPath);
        LoadSessions();
    }

    private void LoadSessions()
    {
        var selectedGameId = GameFilterComboBox.SelectedItem is GameFilterItem selected
            ? selected.GameId
            : null;

        var sessions = _historyStore.ListSessions(selectedGameId);
        SessionsDataGrid.ItemsSource = sessions;

        PopulateGameFilter(selectedGameId);
        UpdateButtons();
    }

    private void PopulateGameFilter(string? preserveSelectionGameId)
    {
        var allSessions = _historyStore.ListSessions();
        var gameGroups = allSessions
            .GroupBy(s => new { s.GameId, s.GameName })
            .Select(g => new GameFilterItem(g.Key.GameId, $"{g.Key.GameName} ({g.Count()})"))
            .OrderBy(g => g.DisplayName)
            .ToList();

        gameGroups.Insert(0, new GameFilterItem(null, "Todos los juegos"));

        GameFilterComboBox.ItemsSource = gameGroups;
        GameFilterComboBox.DisplayMemberPath = nameof(GameFilterItem.DisplayName);

        if (preserveSelectionGameId is not null)
        {
            var match = gameGroups.FirstOrDefault(g => g.GameId == preserveSelectionGameId);
            if (match is not null)
            {
                GameFilterComboBox.SelectedItem = match;
                return;
            }
        }

        GameFilterComboBox.SelectedIndex = 0;
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadSessions();
    }

    private void GameFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GameFilterComboBox.SelectedItem is not GameFilterItem selected)
        {
            return;
        }

        SessionsDataGrid.ItemsSource = _historyStore.ListSessions(selected.GameId);
        UpdateButtons();
    }

    private void SessionsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var hasSelection = SessionsDataGrid.SelectedItem is SessionSummary;
        ViewDetailButton.IsEnabled = hasSelection;
        ExportButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
    }

    private void ViewDetailButton_Click(object sender, RoutedEventArgs e)
    {
        if (SessionsDataGrid.SelectedItem is not SessionSummary summary)
        {
            return;
        }

        var resultsWindow = new SessionResultsWindow(summary) { Owner = this };
        resultsWindow.ShowDialog();
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (SessionsDataGrid.SelectedItem is not SessionSummary summary)
        {
            return;
        }

        SessionResultsWindow.ExportSummary(summary);
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (SessionsDataGrid.SelectedItem is not SessionSummary summary)
        {
            return;
        }

        var result = MessageBox.Show(
            $"¿Eliminar la sesión del {summary.StartedAt:g} para \"{summary.GameName}\"?",
            "Confirmar eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (_historyStore.DeleteSession(summary))
        {
            LoadSessions();
        }
        else
        {
            MessageBox.Show("No se pudo eliminar la sesión.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private sealed record GameFilterItem(string? GameId, string DisplayName);
}
