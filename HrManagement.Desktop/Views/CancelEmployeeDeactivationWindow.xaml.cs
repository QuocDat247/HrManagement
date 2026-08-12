using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;
using System.Windows;

namespace HrManagement.Desktop.Views;

public partial class CancelEmployeeDeactivationWindow
    : Window
{
    private readonly CancelEmployeeDeactivationViewModel
        _viewModel;

    public EmployeeStatus? SelectedRestoredStatus
    {
        get;
        private set;
    }

    public CancelEmployeeDeactivationWindow(
        CancelEmployeeDeactivationViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.ConfirmSucceeded +=
            ViewModel_ConfirmSucceeded;
    }

    public void LoadEmployee(Employee employee)
    {
        _viewModel.LoadEmployee(employee);
    }

    private void ViewModel_ConfirmSucceeded(
        object? sender,
        EventArgs e)
    {
        SelectedRestoredStatus =
            _viewModel
                .SelectedStatusOption
                .Status;

        DialogResult = true;
    }
}
