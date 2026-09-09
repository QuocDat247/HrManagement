using HrManagement.Desktop.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HrManagement.Desktop.Views;

public partial class AccountManagementWorkspaceView
    : UserControl
{
    public AccountManagementWorkspaceView()
    {
        InitializeComponent();
    }

    private async void AccountManagementWorkspaceView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext
            is AccountManagementWorkspaceViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}
