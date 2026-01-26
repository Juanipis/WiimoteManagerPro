using System.Windows;
using WiimoteManager.ViewModels;

namespace WiimoteManager.Views
{
    public partial class MappingWindow : Window
    {
        public MappingWindow(MappingViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}