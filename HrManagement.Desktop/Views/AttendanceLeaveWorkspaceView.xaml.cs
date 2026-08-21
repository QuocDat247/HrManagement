using System.Windows;
using System.Windows.Controls;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Desktop.Views;

public partial class AttendanceLeaveWorkspaceView
    : UserControl
{
    public AttendanceLeaveWorkspaceView()
    {
        InitializeComponent();
    }

    private async void AttendanceLeaveWorkspaceView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is
            AttendanceLeaveWorkspaceViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}
