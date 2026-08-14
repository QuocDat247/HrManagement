using HrManagement.Desktop.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HrManagement.Desktop.Views;

public partial class DepartmentsView
    : UserControl
{
    public DepartmentsView()
    {
        InitializeComponent();
    }

    private async void DepartmentsView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext
            is DepartmentsViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}
