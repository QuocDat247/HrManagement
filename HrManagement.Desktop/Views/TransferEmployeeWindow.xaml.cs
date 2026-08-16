using System.Windows;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.Views;

public partial class TransferEmployeeWindow
    : Window
{
    private readonly TransferEmployeeViewModel
        _viewModel;

    public TransferEmployeeWindow(
        TransferEmployeeViewModel viewModel)
    {
        InitializeComponent();

        _viewModel =
            viewModel;

        DataContext =
            _viewModel;

        _viewModel.SaveSucceeded +=
            ViewModel_SaveSucceeded;

        Loaded +=
            TransferEmployeeWindow_Loaded;
    }

    public void LoadEmployee(
        Employee employee)
    {
        _viewModel.LoadEmployee(
            employee);
    }

    private async void TransferEmployeeWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await _viewModel
            .LoadOrganizationOptionsAsync();
    }

    private void ViewModel_SaveSucceeded(
        object? sender,
        EventArgs e)
    {
        DialogResult =
            true;
    }

    protected override void OnClosed(
        EventArgs e)
    {
        _viewModel.SaveSucceeded -=
            ViewModel_SaveSucceeded;

        Loaded -=
            TransferEmployeeWindow_Loaded;

        base.OnClosed(
            e);
    }
}
