using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;
using System.Windows;

namespace HrManagement.Desktop.Views;

public partial class EmployeeEmploymentHistoryWindow
    : Window
{
    private readonly EmployeeEmploymentHistoryViewModel
        _viewModel;

    private Employee? _employee;

    private bool _historyLoaded;

    public EmployeeEmploymentHistoryWindow(
        EmployeeEmploymentHistoryViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;
    }

    public void LoadEmployee(
        Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        _employee = employee;
        _historyLoaded = false;
    }

    private async void EmployeeEmploymentHistoryWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (_historyLoaded
            || _employee is null)
        {
            return;
        }

        _historyLoaded = true;

        await _viewModel.LoadAsync(
            _employee);
    }
}
