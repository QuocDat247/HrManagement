using System.Windows;
using System.Windows.Controls;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Desktop.Views;

public partial class OvertimeWorkspaceView
    : UserControl
{
    public OvertimeWorkspaceView()
    {
        InitializeComponent();
    }

    private async void OvertimeWorkspaceView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is
            OvertimeWorkspaceViewModel viewModel)
        {
            await viewModel.LoadCommand
                .ExecuteAsync(
                    null);
        }
    }
}
