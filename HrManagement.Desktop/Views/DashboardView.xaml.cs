using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Desktop.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();

        Loaded += DashboardView_Loaded;
    }

    private async void DashboardView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }

    private void DataGrid_PreviewMouseWheel(
    object sender,
    MouseWheelEventArgs e)
    {
        double newOffset =
            DashboardScrollViewer.VerticalOffset - e.Delta;

        DashboardScrollViewer.ScrollToVerticalOffset(newOffset);

        e.Handled = true;
    }
}
