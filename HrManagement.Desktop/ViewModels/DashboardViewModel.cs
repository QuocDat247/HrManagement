using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Dashboard;
using HrManagement.Desktop.Services;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly IDashboardService _dashboardService;

    [ObservableProperty]
    private int totalEmployees;

    [ObservableProperty]
    private int activeEmployees;

    [ObservableProperty]
    private int employeesOnLeave;

    [ObservableProperty]
    private int inactiveEmployees;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private int employeesMissingProfileInformation;

    [ObservableProperty]
    private IReadOnlyList<RecentEmployee> recentEmployees =
    Array.Empty<RecentEmployee>();

    [ObservableProperty]
    private IReadOnlyList<DepartmentEmployeeSummary> departments =
    Array.Empty<DepartmentEmployeeSummary>();

    private readonly IEmployeeNavigationService
    _employeeNavigationService;

    private bool CanShowProfileCompletionRequired()
    {
        return EmployeesMissingProfileInformation > 0;
    }

    private Task ShowProfileCompletionRequiredAsync()
    {
        return _employeeNavigationService
            .ShowEmployeesRequiringProfileCompletionAsync();
    }


    public string Title => "Tổng quan";

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand ShowProfileCompletionRequiredCommand { get; }

    public DashboardViewModel(
    IDashboardService dashboardService,
    IEmployeeNavigationService employeeNavigationService)
    {
        _dashboardService = dashboardService;
        _employeeNavigationService = employeeNavigationService;

        LoadCommand =
            new AsyncRelayCommand(LoadAsync);

        ShowProfileCompletionRequiredCommand =
            new AsyncRelayCommand(
                ShowProfileCompletionRequiredAsync,
                CanShowProfileCompletionRequired);
    }



    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            DashboardSummary summary =
                await _dashboardService.GetSummaryAsync();

            TotalEmployees = summary.TotalEmployees;
            ActiveEmployees = summary.ActiveEmployees;
            EmployeesOnLeave = summary.EmployeesOnLeave;
            InactiveEmployees = summary.InactiveEmployees;
            EmployeesMissingProfileInformation =
            summary.EmployeesMissingProfileInformation;
            ShowProfileCompletionRequiredCommand
            .NotifyCanExecuteChanged();
            RecentEmployees = summary.RecentEmployees;
            Departments = summary.Departments;
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể tải dữ liệu Dashboard.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
