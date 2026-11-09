using System.Windows;

namespace WiimoteManager;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        // Add global exception handler to catch and display startup errors
        DispatcherUnhandledException += (s, e) =>
        {
            MessageBox.Show(
                $"Application Error:\n\n{e.Exception.GetType().Name}\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
                "Unhandled Exception",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = false; // Exit application after showing error
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            Exception ex = (Exception)e.ExceptionObject;
            MessageBox.Show(
                $"Fatal Error:\n\n{ex.GetType().Name}\n\n{ex.Message}",
                "Unhandled Exception",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };
    }
}

