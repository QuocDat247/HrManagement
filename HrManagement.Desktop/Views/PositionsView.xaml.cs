using HrManagement.Desktop.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HrManagement.Desktop.Views;

public partial class PositionsView
    : UserControl
{
    public PositionsView()
    {
        InitializeComponent();
    }

    private async void PositionsView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext
            is PositionsViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}
