using PitStopVR.Core.Models;
using PitStopVR.Telemetry.Configuration;
using PitStopVR.Telemetry.Diagnostics;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PitStopVR.App;

public partial class EcosystemCheckWindow : Window
{
    private readonly string _appDataPath;
    private readonly MachineProfile _machine;

    public EcosystemCheckWindow(string appDataPath, MachineProfile machine)
    {
        InitializeComponent();
        _appDataPath = appDataPath;
        _machine = machine;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async Task RefreshAsync()
    {
        DependenciesPanel.Children.Clear();
        DependenciesPanel.Children.Add(CreateStatusText("Verificando...", Brushes.Gray));

        try
        {
            var settings = new TelemetrySettingsStore(_appDataPath).Load();
            var service = new EcosystemCheckService(settings);
            var dependencies = await service.CheckAllAsync(_machine);

            DependenciesPanel.Children.Clear();
            foreach (var dependency in dependencies)
            {
                DependenciesPanel.Children.Add(CreateDependencyCard(dependency));
            }
        }
        catch (Exception ex)
        {
            DependenciesPanel.Children.Clear();
            DependenciesPanel.Children.Add(CreateStatusText($"Error al verificar: {ex.Message}", Brushes.Red));
        }
    }

    private static Border CreateDependencyCard(EcosystemDependency dependency)
    {
        var statusColor = dependency.IsAvailable ? Brushes.Green : Brushes.OrangeRed;
        var statusText = dependency.IsAvailable ? "Detectado" : "No detectado";

        var title = new TextBlock
        {
            Text = dependency.Name,
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 4)
        };

        var status = new TextBlock
        {
            Text = statusText,
            Foreground = statusColor,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        };

        var description = new TextBlock
        {
            Text = dependency.Description,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };

        var panel = new StackPanel();
        panel.Children.Add(title);
        panel.Children.Add(status);
        panel.Children.Add(description);

        if (!string.IsNullOrWhiteSpace(dependency.Path))
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"Ruta: {dependency.Path}",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.Gray,
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 8)
            });
        }

        if (!dependency.IsAvailable)
        {
            if (!string.IsNullOrWhiteSpace(dependency.InstallHelpText))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = dependency.InstallHelpText,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8)
                });
            }

            var link = new Hyperlink(new Run("Cómo instalar / descargar"))
            {
                NavigateUri = new Uri(dependency.DownloadUrl)
            };
            link.RequestNavigate += (_, e) =>
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            };

            panel.Children.Add(new TextBlock(link) { Margin = new Thickness(0, 0, 0, 4) });
        }

        return new Border
        {
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 12),
            Child = panel
        };
    }

    private static TextBlock CreateStatusText(string text, Brush color)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = color,
            FontStyle = FontStyles.Italic,
            Margin = new Thickness(0, 8, 0, 0)
        };
    }
}
