using HrManagement.Desktop.ViewModels;
using System.Windows;

namespace HrManagement.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }
}