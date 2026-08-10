using System.Windows;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Desktop.Views;

public partial class AddEmployeeWindow : Window
{
    private readonly AddEmployeeViewModel _viewModel;

    public AddEmployeeWindow(AddEmployeeViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.SaveSucceeded += ViewModel_SaveSucceeded;
    }

    private void ViewModel_SaveSucceeded(
        object? sender,
        EventArgs e)
    {
        DialogResult = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.SaveSucceeded -= ViewModel_SaveSucceeded;

        base.OnClosed(e);
    }
}
