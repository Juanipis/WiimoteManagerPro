using System.Windows;
using WiimoteManager.ViewModels;

namespace WiimoteManager;

public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _viewModel = new MainViewModel();
            _viewModel.SetWindow(this); // CRITICAL: Pass window for Raw Input
            DataContext = _viewModel;
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel?.Dispose();
    }
}
