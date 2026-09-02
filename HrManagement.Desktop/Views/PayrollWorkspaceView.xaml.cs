using System.Windows;
using System.Windows.Controls;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Desktop.Views;

public partial class PayrollWorkspaceView
    : UserControl
{
    public PayrollWorkspaceView()
    {
        InitializeComponent();
    }

    private async void PayrollWorkspaceView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is
            PayrollWorkspaceViewModel viewModel)
        {
            await viewModel
                .LoadCommand
                .ExecuteAsync(
                    null);
        }
    }
}
