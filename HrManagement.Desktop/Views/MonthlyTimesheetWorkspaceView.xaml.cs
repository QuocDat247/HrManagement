using System.Windows;
using System.Windows.Controls;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Desktop.Views;

public partial class MonthlyTimesheetWorkspaceView
    : UserControl
{
    public MonthlyTimesheetWorkspaceView()
    {
        InitializeComponent();
    }

    private async void MonthlyTimesheetWorkspaceView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is
            MonthlyTimesheetWorkspaceViewModel viewModel)
        {
            await viewModel.LoadCommand
                .ExecuteAsync(
                    null);
        }
    }
}
