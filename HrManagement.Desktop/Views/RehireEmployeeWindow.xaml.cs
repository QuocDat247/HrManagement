using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;
using System.Windows;

namespace HrManagement.Desktop.Views;

public partial class RehireEmployeeWindow
    : Window
{
    private readonly RehireEmployeeViewModel
        _viewModel;

    public DateOnly? SelectedRehireDate
    {
        get;
        private set;
    }

    public EmployeeStatus? SelectedRehireStatus
    {
        get;
        private set;
    }

    public RehireEmployeeWindow(
        RehireEmployeeViewModel viewModel)
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
        SelectedRehireDate =
            DateOnly.FromDateTime(
                _viewModel.RehireDate);

        SelectedRehireStatus =
            _viewModel
                .SelectedStatusOption
                .Status;

        DialogResult = true;
    }
}
