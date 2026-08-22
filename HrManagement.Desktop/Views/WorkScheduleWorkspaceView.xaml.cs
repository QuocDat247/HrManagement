using System.Windows;
using System.Windows.Controls;
using HrManagement.Desktop.ViewModels;
using System.Windows.Input;

namespace HrManagement.Desktop.Views;

public partial class WorkScheduleWorkspaceView
    : UserControl
{
    public WorkScheduleWorkspaceView()
    {
        InitializeComponent();
    }

    private async void WorkScheduleWorkspaceView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is
            WorkScheduleWorkspaceViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }

    private void WeeklyDaysDataGrid_PreviewMouseWheel(
    object sender,
    MouseWheelEventArgs e)
    {
        e.Handled =
            true;

        var forwardedEvent =
            new MouseWheelEventArgs(
                e.MouseDevice,
                e.Timestamp,
                e.Delta)
            {
                RoutedEvent =
                    Mouse.MouseWheelEvent,

                Source =
                    sender
            };

        WeeklyScheduleScrollViewer.RaiseEvent(
            forwardedEvent);
    }
}
