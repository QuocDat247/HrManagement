using System.Windows;
using HrManagement.Desktop.Views;
using HrManagement.Domain.Employees;
using Microsoft.Extensions.DependencyInjection;

namespace HrManagement.Desktop.Services;

public sealed class EmployeeDialogService : IEmployeeDialogService
{
    private readonly IServiceProvider _serviceProvider;

    public EmployeeDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public bool ShowAddEmployeeDialog()
    {
        var window =
            _serviceProvider.GetRequiredService<AddEmployeeWindow>();

        if (System.Windows.Application.Current.MainWindow is not null)
        {
            window.Owner =
                System.Windows.Application.Current.MainWindow;
        }

        return window.ShowDialog() == true;
    }

    public bool ShowEditEmployeeDialog(Employee employee)
    {
        var window =
            _serviceProvider.GetRequiredService<EditEmployeeWindow>();

        window.LoadEmployee(employee);

        if (System.Windows.Application.Current.MainWindow is not null)
        {
            window.Owner =
                System.Windows.Application.Current.MainWindow;
        }

        return window.ShowDialog() == true;
    }

    public DateOnly? ShowDeactivateEmployeeDialog(
    Employee employee)
    {
        var window =
            _serviceProvider
                .GetRequiredService<DeactivateEmployeeWindow>();

        window.LoadEmployee(employee);

        if (System.Windows.Application.Current.MainWindow
            is not null)
        {
            window.Owner =
                System.Windows.Application.Current.MainWindow;
        }

        bool? result =
            window.ShowDialog();

        return result == true
            ? window.SelectedTerminationDate
            : null;
    }
}
