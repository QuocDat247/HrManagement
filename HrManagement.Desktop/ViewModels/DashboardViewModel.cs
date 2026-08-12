using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Dashboard;
using HrManagement.Desktop.Services;
using HrManagement.Application.Dashboard.Analytics;

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
    private int selectedAnalyticsYear =
    DateTime.Today.Year;

    [ObservableProperty]
    private WorkforceAnalyticsGroupingOption
        selectedAnalyticsGroupingOption = default!;

    [ObservableProperty]
    private int analyticsBeginningHeadcount;

    [ObservableProperty]
    private int analyticsEndingHeadcount;

    [ObservableProperty]
    private int totalNewHires;

    [ObservableProperty]
    private int totalSeparations;

    [ObservableProperty]
    private int workforceNetChange;

    [ObservableProperty]
    private decimal workforceAverageHeadcount;

    [ObservableProperty]
    private decimal workforceTurnoverRate;

    [ObservableProperty]
    private int employeesWithUnknownTerminationDate;

    [ObservableProperty]
    private IReadOnlyList<WorkforceMovementChartItem>
        workforceMovementItems =
            Array.Empty<WorkforceMovementChartItem>();

    [ObservableProperty]
    private bool isAnalyticsLoading;

    [ObservableProperty]
    private string? analyticsErrorMessage;

    [ObservableProperty]
    private IReadOnlyList<RecentEmployee> recentEmployees =
    Array.Empty<RecentEmployee>();

    [ObservableProperty]
    private IReadOnlyList<DepartmentEmployeeSummary> departments =
    Array.Empty<DepartmentEmployeeSummary>();

    [ObservableProperty]
    private int workforceMovementChartMaximum = 1;

    [ObservableProperty]
    private bool hasWorkforceMovementData;

    public int WorkforceMovementChartMidpoint =>
        (int)Math.Ceiling(
            WorkforceMovementChartMaximum / 2m);

    public string WorkforceNetChangeDisplay =>
            WorkforceNetChange > 0
                ? $"+{WorkforceNetChange}"
                : WorkforceNetChange.ToString();


    partial void OnWorkforceNetChangeChanged(
    int value)
    {
        OnPropertyChanged(
            nameof(WorkforceNetChangeDisplay));
    }

    partial void OnWorkforceMovementChartMaximumChanged(
        int value)
    {
        OnPropertyChanged(
            nameof(WorkforceMovementChartMidpoint));
    }

    private readonly IEmployeeNavigationService
    _employeeNavigationService;

    private readonly IWorkforceAnalyticsService
    _workforceAnalyticsService;

    private bool CanShowProfileCompletionRequired()
    {
        return EmployeesMissingProfileInformation > 0;
    }

    private Task ShowProfileCompletionRequiredAsync()
    {
        return _employeeNavigationService
            .ShowEmployeesRequiringProfileCompletionAsync();
    }

    private static string BuildPeriodLabel(
    WorkforceAnalyticsGrouping grouping,
    int periodNumber)
    {
        return grouping ==
            WorkforceAnalyticsGrouping.Monthly
                ? $"T{periodNumber}"
                : $"Q{periodNumber}";
    }

    private static string BuildPeriodDisplayName(
        WorkforceAnalyticsGrouping grouping,
        int periodNumber)
    {
        return grouping ==
            WorkforceAnalyticsGrouping.Monthly
                ? $"Tháng {periodNumber}"
                : $"Quý {periodNumber}";
    }


    public string Title => "Tổng quan";

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand ShowProfileCompletionRequiredCommand { get; }

    public IAsyncRelayCommand RefreshAnalyticsCommand
    {
        get;
    }

    public IReadOnlyList<int> AnalyticsYears { get; } =
    Enumerable.Range(
            DateTime.Today.Year - 9,
            10)
        .Reverse()
        .ToList();

    public DashboardViewModel(
    IDashboardService dashboardService,
    IEmployeeNavigationService employeeNavigationService,
    IWorkforceAnalyticsService workforceAnalyticsService)
    {
        _dashboardService = dashboardService;
        _employeeNavigationService = employeeNavigationService;
        _workforceAnalyticsService = workforceAnalyticsService;

        SelectedAnalyticsGroupingOption =
        AnalyticsGroupingOptions[0];

        LoadCommand =
            new AsyncRelayCommand(LoadAsync);

        ShowProfileCompletionRequiredCommand =
            new AsyncRelayCommand(
                ShowProfileCompletionRequiredAsync,
                CanShowProfileCompletionRequired);

        RefreshAnalyticsCommand =
            new AsyncRelayCommand(
                LoadAnalyticsAsync);
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

            await LoadAnalyticsAsync();
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

    public IReadOnlyList<WorkforceAnalyticsGroupingOption>
    AnalyticsGroupingOptions
        { get; } =
    [
        new(
            "Theo tháng",
            WorkforceAnalyticsGrouping.Monthly),

        new(
            "Theo quý",
            WorkforceAnalyticsGrouping.Quarterly)
    ];

    public async Task LoadAnalyticsAsync()
    {
        try
        {
            IsAnalyticsLoading = true;
            AnalyticsErrorMessage = null;
            WorkforceMovementSummary summary =
                await _workforceAnalyticsService
                    .GetWorkforceMovementAsync(
                        SelectedAnalyticsYear,
                        SelectedAnalyticsGroupingOption
                            .Grouping);
            AnalyticsBeginningHeadcount =
                summary.BeginningHeadcount;
            AnalyticsEndingHeadcount =
                summary.EndingHeadcount;
            TotalNewHires =
                summary.TotalNewHires;
            TotalSeparations =
                summary.TotalSeparations;
            WorkforceNetChange =
                summary.NetChange;
            WorkforceAverageHeadcount =
                summary.AverageHeadcount;
            WorkforceTurnoverRate =
                summary.TurnoverRate;
            EmployeesWithUnknownTerminationDate =
                summary.EmployeesWithUnknownTerminationDate;

            List<WorkforceMovementChartItem> items =
                summary.Periods
                    .Select(period =>
                        new WorkforceMovementChartItem(
                            PeriodNumber:
                                period.PeriodNumber,
                            Label:
                                BuildPeriodLabel(
                                    summary.Grouping,
                                    period.PeriodNumber),
                            DisplayName:
                                BuildPeriodDisplayName(
                                    summary.Grouping,
                                    period.PeriodNumber),
                            NewHires:
                                period.NewHires,
                            Separations:
                                period.Separations,
                            NetChange:
                                period.NetChange,
                            TurnoverRate:
                                period.TurnoverRate))
                    .ToList();
                        WorkforceMovementItems = items;
                        HasWorkforceMovementData =
                            items.Any(item =>
                            item.NewHires > 0
                            || item.Separations > 0);
                        WorkforceMovementChartMaximum =
                            Math.Max(
                                1,
                                items
                                    .Select(item =>
                                        Math.Max(
                                            item.NewHires,
                                            item.Separations))
                                    .DefaultIfEmpty(0)
                                    .Max());
        }
        catch (Exception)
        {
            WorkforceMovementItems =
                Array.Empty<WorkforceMovementChartItem>();
            AnalyticsErrorMessage =
                "Không thể tải dữ liệu biến động nhân sự.";
            HasWorkforceMovementData = false;
        }
        finally
        {
            IsAnalyticsLoading = false;
        }
    }
}
