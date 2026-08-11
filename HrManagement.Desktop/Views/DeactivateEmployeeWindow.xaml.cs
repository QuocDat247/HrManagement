using System.Windows;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.Views;

public partial class DeactivateEmployeeWindow
    : Window
{
    private readonly DeactivateEmployeeViewModel _viewModel;

    public DeactivateEmployeeWindow(
        DeactivateEmployeeViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.ConfirmSucceeded +=
            ViewModel_ConfirmSucceeded;
    }

    public DateOnly SelectedTerminationDate =>
        DateOnly.FromDateTime(
            _viewModel.TerminationDate);

    public void LoadEmployee(Employee employee)
    {
        _viewModel.LoadEmployee(employee);
    }

    private void ViewModel_ConfirmSucceeded(
        object? sender,
        EventArgs e)
    {
        DialogResult = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.ConfirmSucceeded -=
            ViewModel_ConfirmSucceeded;

        base.OnClosed(e);
    }
}
