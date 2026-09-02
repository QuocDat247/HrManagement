using System.Windows;
using System.Windows.Controls;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Desktop.Views;

public partial class SettingsView
    : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void SettingsView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is
            SettingsViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(
                null);
        }
    }
}
