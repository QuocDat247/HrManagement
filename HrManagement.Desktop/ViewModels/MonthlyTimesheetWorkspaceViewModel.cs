using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Attendance.Timesheets;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class MonthlyTimesheetWorkspaceViewModel
    : ObservableObject
{
    private readonly IMonthlyTimesheetQueryService
        _queryService;

    private readonly TimeProvider
        _timeProvider;

    [ObservableProperty]
    private IReadOnlyList<int> yearOptions =
        [];

    [ObservableProperty]
    private IReadOnlyList<int> monthOptions =
        [];

    [ObservableProperty]
    private int selectedYear;

    [ObservableProperty]
    private int selectedMonth;

    [ObservableProperty]
    private MonthlyTimesheetReadModel? timesheet;

    [ObservableProperty]
    private IReadOnlyList<MonthlyTimesheetRowViewModel>
        rows =
            [];

    [ObservableProperty]
    private string periodStatusText =
        "Chưa tải";

    [ObservableProperty]
    private string dataSourceText =
        "—";

    [ObservableProperty]
    private int employeeCount;

    [ObservableProperty]
    private int finalizedDayCount;

    [ObservableProperty]
    private int pendingDayCount;

    [ObservableProperty]
    private int correctedDayCount;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    public MonthlyTimesheetWorkspaceViewModel(
        IMonthlyTimesheetQueryService queryService,
        TimeProvider timeProvider)
    {
        _queryService =
            queryService;

        _timeProvider =
            timeProvider;

        DateTimeOffset localNow =
            _timeProvider.GetLocalNow();

        SelectedYear =
            localNow.Year;

        SelectedMonth =
            localNow.Month;

        YearOptions =
            CreateYearOptions(
                localNow.Year);

        MonthOptions =
            Enumerable
                .Range(
                    1,
                    12)
                .ToArray();

        LoadCommand =
            new AsyncRelayCommand(
                LoadAsync);

        RefreshCommand =
            LoadCommand;
    }

    public IAsyncRelayCommand LoadCommand
    {
        get;
    }

    public IAsyncRelayCommand RefreshCommand
    {
        get;
    }

    private async Task LoadAsync()
    {
        ErrorMessage =
            null;

        if (SelectedYear is < 2000 or > 9999)
        {
            ClearLoadedData();

            ErrorMessage =
                "Năm bảng công không hợp lệ.";

            return;
        }

        if (SelectedMonth is < 1 or > 12)
        {
            ClearLoadedData();

            ErrorMessage =
                "Tháng bảng công phải từ 1 đến 12.";

            return;
        }

        IsLoading =
            true;

        try
        {
            MonthlyTimesheetReadModel readModel =
                await _queryService
                    .GetAsync(
                        SelectedYear,
                        SelectedMonth);

            ApplyReadModel(
                readModel);
        }
        catch (Exception exception)
        {
            ClearLoadedData();

            ErrorMessage =
                exception.Message;
        }
        finally
        {
            IsLoading =
                false;
        }
    }

    private void ApplyReadModel(
        MonthlyTimesheetReadModel readModel)
    {
        Timesheet =
            readModel;

        MonthlyTimesheetRowViewModel[] loadedRows =
            readModel
                .Items
                .Select(
                    MonthlyTimesheetRowViewModel.From)
                .OrderBy(
                    row =>
                        row.EmployeeCode)
                .ThenBy(
                    row =>
                        row.WorkDate)
                .ToArray();

        Rows =
            loadedRows;

        PeriodStatusText =
            readModel.IsClosed
                ? "Đã đóng"
                : "Đang mở";

        DataSourceText =
            readModel.IsClosed
                ? "Bản chụp đã đóng"
                : "Dữ liệu trực tiếp";

        EmployeeCount =
            loadedRows
                .Select(
                    row =>
                        row.EmployeeId)
                .Distinct()
                .Count();

        FinalizedDayCount =
            loadedRows.Count(
                row =>
                    row.IsFinalized);

        PendingDayCount =
            loadedRows.Length
            - FinalizedDayCount;

        CorrectedDayCount =
            loadedRows.Count(
                row =>
                    row.CorrectionRevision > 0);
    }

    private void ClearLoadedData()
    {
        Timesheet =
            null;

        Rows =
            [];

        PeriodStatusText =
            "Chưa tải";

        DataSourceText =
            "—";

        EmployeeCount =
            0;

        FinalizedDayCount =
            0;

        PendingDayCount =
            0;

        CorrectedDayCount =
            0;
    }

    private static IReadOnlyList<int> CreateYearOptions(
        int currentYear)
    {
        int firstYear =
            Math.Max(
                2000,
                currentYear - 2);

        int lastYear =
            Math.Min(
                9999,
                currentYear + 3);

        return Enumerable
            .Range(
                firstYear,
                lastYear - firstYear + 1)
            .ToArray();
    }
}
