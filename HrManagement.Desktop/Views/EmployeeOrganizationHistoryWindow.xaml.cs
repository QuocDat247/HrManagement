using System.Windows;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.Views;

public partial class EmployeeOrganizationHistoryWindow
    : Window
{
    private readonly EmployeeOrganizationHistoryViewModel
        _viewModel;

    private Employee? _employee;

    private bool _historyLoaded;

    public EmployeeOrganizationHistoryWindow(
        EmployeeOrganizationHistoryViewModel viewModel)
    {
        InitializeComponent();

        _viewModel =
            viewModel;

        DataContext =
            viewModel;
    }

    public void LoadEmployee(
        Employee employee)
    {
        ArgumentNullException.ThrowIfNull(
            employee);

        _employee =
            employee;

        _historyLoaded =
            false;
    }

    private async void EmployeeOrganizationHistoryWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (_historyLoaded
            || _employee is null)
        {
            return;
        }

        _historyLoaded =
            true;

        await _viewModel.LoadAsync(
            _employee);
    }
}
