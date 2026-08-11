using HrManagement.Desktop.Navigation;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Desktop.Services;

public sealed class EmployeeNavigationService
    : IEmployeeNavigationService
{
    private readonly INavigationService _navigationService;

    public EmployeeNavigationService(
        INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public async Task ShowEmployeesRequiringProfileCompletionAsync()
    {
        _navigationService.NavigateTo<EmployeesViewModel>();

        if (_navigationService.CurrentViewModel
            is not EmployeesViewModel employeesViewModel)
        {
            throw new InvalidOperationException(
                "Không thể điều hướng đến màn hình Nhân viên.");
        }

        await employeesViewModel
            .ShowProfileCompletionRequiredAsync();
    }
}
