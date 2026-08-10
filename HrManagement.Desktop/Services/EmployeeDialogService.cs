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

    public bool ConfirmDeactivateEmployee(Employee employee)
    {
        string message =
            $"Bạn có chắc muốn ngừng hoạt động nhân viên " +
            $"{employee.EmployeeCode} - {employee.FullName}?";

        if (System.Windows.Application.Current.MainWindow is Window owner)
        {
            return MessageBox.Show(
                owner,
                message,
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        return MessageBox.Show(
            message,
            "Xác nhận",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) == MessageBoxResult.Yes;
    }
}
