using System.Windows;
using System.Windows.Input;
using WiimoteManager.ViewModels;
using WiimoteManager.Services;

namespace WiimoteManager.Views;

public partial class ProfileManagerWindow : Window
{
    public ProfileManagerWindow()
    {
        InitializeComponent();
        DataContext = new ProfileManagerViewModel(new ProfileService());
    }
    
    public ProfileManagerWindow(ProfileManagerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
    
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
        else
        {
            DragMove();
        }
    }
    
    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
    
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

