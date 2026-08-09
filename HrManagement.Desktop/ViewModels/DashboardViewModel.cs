using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Dashboard;

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
    private int contractsExpiringSoon;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    public string Title => "Tổng quan";

    public IAsyncRelayCommand LoadCommand { get; }

    public DashboardViewModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;

        LoadCommand = new AsyncRelayCommand(LoadAsync);
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
            ContractsExpiringSoon = summary.ContractsExpiringSoon;
        }
        catch (Exception)
        {
            ErrorMessage = "Không thể tải dữ liệu Dashboard.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
