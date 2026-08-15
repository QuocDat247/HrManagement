using HrManagement.Desktop.Views;
using HrManagement.Domain.Organization.Departments;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace HrManagement.Desktop.Services.Departments;

public sealed class DepartmentDialogService
    : IDepartmentDialogService
{
    private readonly IServiceProvider
        _serviceProvider;

    public DepartmentDialogService(
        IServiceProvider serviceProvider)
    {
        _serviceProvider =
            serviceProvider;
    }

    public void ShowEmployees(
        Department department)
    {
        ArgumentNullException.ThrowIfNull(
            department);

        OrganizationEmployeesWindow window =
            _serviceProvider
                .GetRequiredService<
                    OrganizationEmployeesWindow>();

        window.Owner =
            System.Windows.Application
                .Current
                .MainWindow;

        window.LoadDepartment(
            department);

        window.ShowDialog();
    }

    public DepartmentEditorDialogResult?
        ShowAddDepartmentDialog()
    {
        DepartmentEditorWindow window =
            CreateEditorWindow();

        window.LoadForAdd();

        bool? result =
            window.ShowDialog();

        return result == true
            ? window.Result
            : null;
    }

    public DepartmentEditorDialogResult?
        ShowEditDepartmentDialog(
            Department department)
    {
        ArgumentNullException.ThrowIfNull(
            department);

        DepartmentEditorWindow window =
            CreateEditorWindow();

        window.LoadForEdit(
            department);

        bool? result =
            window.ShowDialog();

        return result == true
            ? window.Result
            : null;
    }

    public bool ConfirmDeactivateDepartment(
        Department department)
    {
        ArgumentNullException.ThrowIfNull(
            department);

        MessageBoxResult result =
            MessageBox.Show(
                $"Ngừng sử dụng phòng ban \"{department.Name}\"?\n\n"
                + "Phòng ban vẫn được giữ lại trong lịch sử nhưng sẽ không còn dùng cho các lựa chọn mới.",
                "Ngừng sử dụng phòng ban",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

        return result ==
            MessageBoxResult.Yes;
    }

    public bool ConfirmReactivateDepartment(
        Department department)
    {
        ArgumentNullException.ThrowIfNull(
            department);

        MessageBoxResult result =
            MessageBox.Show(
                $"Kích hoạt lại phòng ban \"{department.Name}\"?",
                "Kích hoạt phòng ban",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

        return result ==
            MessageBoxResult.Yes;
    }

    private DepartmentEditorWindow
        CreateEditorWindow()
    {
        DepartmentEditorWindow window =
            _serviceProvider
                .GetRequiredService<
                    DepartmentEditorWindow>();

        window.Owner =
            System.Windows.Application
                .Current
                .MainWindow;

        return window;
    }
}
