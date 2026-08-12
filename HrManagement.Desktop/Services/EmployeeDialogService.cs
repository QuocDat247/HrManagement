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

    public RehireEmployeeDialogResult?
    ShowRehireEmployeeDialog(
        Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        RehireEmployeeWindow window =
            _serviceProvider.GetRequiredService<
                RehireEmployeeWindow>();

        window.LoadEmployee(employee);

        window.Owner =
            System.Windows.Application
                .Current
                .MainWindow;

        bool? result =
            window.ShowDialog();

        if (result != true
            || !window.SelectedRehireDate.HasValue
            || !window.SelectedRehireStatus.HasValue)
        {
            return null;
        }

        return new RehireEmployeeDialogResult(
            window.SelectedRehireDate.Value,
            window.SelectedRehireStatus.Value);
    }

    public void ShowEmploymentHistoryDialog(
    Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        EmployeeEmploymentHistoryWindow window =
            _serviceProvider.GetRequiredService<
                EmployeeEmploymentHistoryWindow>();

        window.LoadEmployee(employee);

        window.Owner =
            System.Windows.Application
                .Current
                .MainWindow;

        window.ShowDialog();
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

    public EmployeeStatus?
    ShowCancelEmployeeDeactivationDialog(
        Employee employee)
{
    ArgumentNullException.ThrowIfNull(employee);

    CancelEmployeeDeactivationWindow window =
        _serviceProvider.GetRequiredService<
            CancelEmployeeDeactivationWindow>();

    window.LoadEmployee(employee);

    window.Owner =
        System.Windows.Application
            .Current
            .MainWindow;

    bool? result =
        window.ShowDialog();

    return result == true
        ? window.SelectedRestoredStatus
        : null;
}
}
