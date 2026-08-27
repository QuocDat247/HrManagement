using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Desktop.Services;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class MonthlyTimesheetWorkspaceViewModel
    : ObservableObject
{
    private readonly IMonthlyTimesheetQueryService
        _queryService;

    private readonly ICloseTimesheetPeriodService
        _closePeriodService;

    private readonly IUserConfirmationService
        _confirmationService;

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
    private string closingReadinessText =
        "Chưa tải";

    [ObservableProperty]
    private string closedAtText =
        "—";

    [ObservableProperty]
    private string closedByText =
        "—";

    [ObservableProperty]
    private bool canAttemptClosePeriod;

    [ObservableProperty]
    private bool isClosingPeriod;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    public MonthlyTimesheetWorkspaceViewModel(
        IMonthlyTimesheetQueryService queryService,
        ICloseTimesheetPeriodService closePeriodService,
        IUserConfirmationService confirmationService,
        TimeProvider timeProvider)
    {
        _queryService =
            queryService;

        _closePeriodService =
            closePeriodService;

        _confirmationService =
            confirmationService;

        _timeProvider =
            timeProvider;

        ClosePeriodCommand =
            new AsyncRelayCommand(
                ClosePeriodAsync,
                CanCloseDisplayedPeriod);

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

    public IAsyncRelayCommand ClosePeriodCommand
    {
        get;
    }

    private async Task ClosePeriodAsync()
    {
        if (!CanCloseDisplayedPeriod()
            || Timesheet is null)
        {
            return;
        }

        int year =
            Timesheet.Year;

        int month =
            Timesheet.Month;

        bool confirmed =
            _confirmationService.Confirm(
                "Xác nhận đóng kỳ công",
                $"Bạn sắp đóng kỳ công tháng {month:00}/{year}.\n\n"
                + "Sau khi đóng, dữ liệu bảng công của kỳ sẽ được chốt thành bản chụp bất biến "
                + "và các thao tác chấm công, điều chỉnh hoặc tính lại trong kỳ sẽ bị khóa.\n\n"
                + "Bạn có chắc chắn muốn tiếp tục?");

        if (!confirmed)
        {
            return;
        }

        ErrorMessage =
            null;

        IsClosingPeriod =
            true;

        ClosePeriodCommand
            .NotifyCanExecuteChanged();

        try
        {
            CloseTimesheetPeriodResult result =
                await _closePeriodService
                    .CloseAsync(
                        new CloseTimesheetPeriodRequest(
                            year,
                            month));

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    string.IsNullOrWhiteSpace(
                        result.ErrorMessage)
                        ? "Không thể đóng kỳ công."
                        : result.ErrorMessage;

                return;
            }

            await LoadAsync();
        }
        catch (Exception exception)
        {
            ErrorMessage =
                exception.Message;
        }
        finally
        {
            IsClosingPeriod =
                false;

            ClosePeriodCommand
                .NotifyCanExecuteChanged();
        }
    }

    private bool CanCloseDisplayedPeriod()
    {
        return
            !IsLoading
            && !IsClosingPeriod
            && CanAttemptClosePeriod
            && Timesheet is not null
            && !Timesheet.IsClosed
            && Timesheet.Year == SelectedYear
            && Timesheet.Month == SelectedMonth;
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

        ClosePeriodCommand
            .NotifyCanExecuteChanged();

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

            ClosePeriodCommand
                .NotifyCanExecuteChanged();
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

        CanAttemptClosePeriod =
            !readModel.IsClosed
            && PendingDayCount == 0;

        ClosingReadinessText =
            GetClosingReadinessText(
                readModel,
                PendingDayCount);

        ClosedAtText =
            FormatClosedAt(
                readModel.ClosedAtUtc);

        ClosedByText =
            !string.IsNullOrWhiteSpace(
                readModel.ClosedByUsername)
                    ? readModel.ClosedByUsername
                    : "—";

        ClosePeriodCommand
            .NotifyCanExecuteChanged();
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

        ClosingReadinessText =
            "Chưa tải";

        ClosedAtText =
            "—";

        ClosedByText =
            "—";

        CanAttemptClosePeriod =
            false;

        ClosePeriodCommand
            .NotifyCanExecuteChanged();
    }

    partial void OnSelectedYearChanged(
    int value)
    {
        ClosePeriodCommand?
            .NotifyCanExecuteChanged();
    }

    partial void OnSelectedMonthChanged(
        int value)
    {
        ClosePeriodCommand?
            .NotifyCanExecuteChanged();
    }

    private static string GetClosingReadinessText(
    MonthlyTimesheetReadModel readModel,
    int pendingDayCount)
    {
        if (readModel.IsClosed)
        {
            return "Kỳ đã đóng";
        }

        if (pendingDayCount > 0)
        {
            return
                $"Còn {pendingDayCount} ngày chờ xử lý";
        }

        return "Sẵn sàng kiểm tra đóng kỳ";
    }

    private string FormatClosedAt(
        DateTime? closedAtUtc)
    {
        if (!closedAtUtc.HasValue)
        {
            return "—";
        }

        DateTime utcValue =
            DateTime.SpecifyKind(
                closedAtUtc.Value,
                DateTimeKind.Utc);

        DateTime localValue =
            TimeZoneInfo.ConvertTimeFromUtc(
                utcValue,
                _timeProvider.LocalTimeZone);

        return localValue.ToString(
            "dd/MM/yyyy HH:mm");
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
