using System.Windows;
using WiimoteManager.ViewModels;

namespace WiimoteManager.Views;

public partial class ButtonTestWindow : Window
{
    public ButtonTestWindow()
    {
        InitializeComponent();
    }

    public ButtonTestWindow(ButtonTestViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
