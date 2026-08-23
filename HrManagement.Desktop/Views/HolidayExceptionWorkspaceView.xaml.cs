using System.Windows;
using System.Windows.Controls;
using HrManagement.Application.Workspaces.HolidayExceptions;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Desktop.Views;

public partial class HolidayExceptionWorkspaceView
    : UserControl
{
    public HolidayExceptionWorkspaceView()
    {
        InitializeComponent();
    }

    private async void HolidayExceptionWorkspaceView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is
            HolidayExceptionWorkspaceViewModel viewModel)
        {
            await viewModel.LoadCommand
                .ExecuteAsync(
                    null);
        }
    }

    private async void YearSelector_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (!IsLoaded
            || DataContext is not
                HolidayExceptionWorkspaceViewModel viewModel
            || viewModel.IsLoading
            || YearSelector.SelectedItem is not
                int selectedYear)
        {
            return;
        }

        viewModel.SelectedYear =
            selectedYear;

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);
    }

    private async void ScheduleSelector_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (!IsLoaded
            || DataContext is not
                HolidayExceptionWorkspaceViewModel viewModel
            || viewModel.IsLoading)
        {
            return;
        }

        viewModel.SelectedScheduleItem =
            ScheduleSelector.SelectedItem
                as HolidayExceptionWorkspaceScheduleItem;

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);
    }
}
