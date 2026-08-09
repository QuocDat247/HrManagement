using CommunityToolkit.Mvvm.ComponentModel;
using HrManagement.Application.Employees;
using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class EmployeesViewModel : ObservableObject
{
    private readonly IEmployeeService _employeeService;

    [ObservableProperty]
    private IReadOnlyList<Employee> employees =
        Array.Empty<Employee>();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    public string Title => "Nhân viên";

    public EmployeesViewModel(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            Employees =
                await _employeeService.GetEmployeesAsync();
        }
        catch (Exception)
        {
            Employees = Array.Empty<Employee>();
            ErrorMessage = "Không thể tải danh sách nhân viên.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
