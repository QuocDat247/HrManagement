using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Payroll.Calculations;
using HrManagement.Application.Payroll.Periods;
using HrManagement.Desktop.Services;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class PayrollWorkspaceViewModel
    : ObservableObject
{
    private readonly IPayrollPreviewService
        _previewService;

    private readonly IClosedPayrollQueryService
        _closedPayrollQueryService;

    private readonly IClosePayrollPeriodService
        _closePayrollPeriodService;

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
    private PayrollWorkspaceMode mode =
        PayrollWorkspaceMode.Preview;

    [ObservableProperty]
    private IReadOnlyList<PayrollWorkspaceRowViewModel>
        rows =
            [];

    [ObservableProperty]
    private IReadOnlyList<PayrollCurrencySummaryViewModel>
        currencySummaries =
            [];

    [ObservableProperty]
    private IReadOnlyList<string> issueMessages =
        [];

    [ObservableProperty]
    private int employeeCount;

    [ObservableProperty]
    private bool previewIsFinalizable;

    [ObservableProperty]
    private Guid? payrollPeriodId;

    [ObservableProperty]
    private Guid? timesheetPeriodId;

    [ObservableProperty]
    private DateTime? closedAtUtc;

    [ObservableProperty]
    private string? closedByUsername;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isClosing;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string? successMessage;

    public PayrollWorkspaceViewModel(
        IPayrollPreviewService previewService,
        IClosedPayrollQueryService closedPayrollQueryService,
        IClosePayrollPeriodService closePayrollPeriodService,
        IUserConfirmationService confirmationService,
        TimeProvider timeProvider)
    {
        _previewService =
            previewService;

        _closedPayrollQueryService =
            closedPayrollQueryService;

        _closePayrollPeriodService =
            closePayrollPeriodService;

        _confirmationService =
            confirmationService;

        _timeProvider =
            timeProvider;

        DateTimeOffset localNow =
            _timeProvider.GetLocalNow();

        SelectedYear =
            localNow.Year;

        SelectedMonth =
            localNow.Month;

        YearOptions =
            Enumerable
                .Range(
                    localNow.Year - 5,
                    11)
                .Reverse()
                .ToArray();

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

        ClosePayrollCommand =
            new AsyncRelayCommand(
                ClosePayrollAsync,
                CanClosePayroll);
    }

    public IAsyncRelayCommand LoadCommand
    {
        get;
    }

    public IAsyncRelayCommand RefreshCommand
    {
        get;
    }

    public IAsyncRelayCommand ClosePayrollCommand
    {
        get;
    }

    public bool IsPreview =>
        Mode ==
        PayrollWorkspaceMode.Preview;

    public bool IsClosed =>
        Mode ==
        PayrollWorkspaceMode.Closed;

    public bool CanClose =>
        CanClosePayroll();

    public string ModeText =>
        IsClosed
            ? "Đã đóng"
            : PreviewIsFinalizable
                ? "Sẵn sàng đóng"
                : "Bản xem trước";

    public string ClosedInfoText
    {
        get
        {
            if (!IsClosed
                || !ClosedAtUtc.HasValue)
            {
                return string.Empty;
            }

            return
                $"Đóng bởi {ClosedByUsername} lúc "
                + $"{ClosedAtUtc.Value.ToLocalTime():dd/MM/yyyy HH:mm}";
        }
    }

    private async Task LoadAsync()
    {
        if (IsLoading
            || IsClosing)
        {
            return;
        }

        ErrorMessage =
            null;

        SuccessMessage =
            null;

        if (!ValidatePeriod())
        {
            ClearWorkspace();

            return;
        }

        IsLoading =
            true;

        NotifyCommandState();

        try
        {
            ClosedPayrollReadModel? closedPayroll =
                await _closedPayrollQueryService
                    .GetAsync(
                        SelectedYear,
                        SelectedMonth);

            if (closedPayroll is not null)
            {
                LoadClosedPayroll(
                    closedPayroll);

                return;
            }

            PayrollPreview preview =
                await _previewService
                    .GetAsync(
                        SelectedYear,
                        SelectedMonth);

            LoadPreview(
                preview);
        }
        catch (ArgumentException exception)
        {
            ClearWorkspace();

            ErrorMessage =
                CleanArgumentMessage(
                    exception);
        }
        catch (InvalidOperationException exception)
        {
            ClearWorkspace();

            ErrorMessage =
                exception.Message;
        }
        finally
        {
            IsLoading =
                false;

            NotifyCommandState();
        }
    }

    private async Task ClosePayrollAsync()
    {
        if (!CanClosePayroll())
        {
            return;
        }

        bool confirmed =
            _confirmationService.Confirm(
                "Đóng kỳ lương",
                $"Bạn có chắc muốn đóng kỳ lương "
                + $"{SelectedMonth:00}/{SelectedYear}?\n\n"
                + "Sau khi đóng, dữ liệu tiền lương của kỳ này "
                + "sẽ được khóa và không thể thay đổi.");

        if (!confirmed)
        {
            return;
        }

        ErrorMessage =
            null;

        SuccessMessage =
            null;

        IsClosing =
            true;

        NotifyCommandState();

        try
        {
            ClosePayrollPeriodResult result =
                await _closePayrollPeriodService
                    .CloseAsync(
                        new ClosePayrollPeriodRequest(
                            SelectedYear,
                            SelectedMonth));

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể đóng kỳ lương.";

                return;
            }

            SuccessMessage =
                $"Đã đóng kỳ lương "
                + $"{SelectedMonth:00}/{SelectedYear} "
                + $"với {result.SnapshotCount} nhân viên.";

            ClosedPayrollReadModel? closedPayroll =
                await _closedPayrollQueryService
                    .GetAsync(
                        SelectedYear,
                        SelectedMonth);

            if (closedPayroll is null)
            {
                ClearWorkspace();

                ErrorMessage =
                    "Kỳ lương đã được đóng nhưng không thể đọc lại snapshot đã khóa.";

                return;
            }

            LoadClosedPayroll(
                closedPayroll);
        }
        catch (ArgumentException exception)
        {
            ErrorMessage =
                CleanArgumentMessage(
                    exception);
        }
        catch (InvalidOperationException exception)
        {
            ErrorMessage =
                exception.Message;
        }
        finally
        {
            IsClosing =
                false;

            NotifyCommandState();
        }
    }

    private void LoadPreview(
        PayrollPreview preview)
    {
        if (preview.Year !=
                SelectedYear
            || preview.Month !=
                SelectedMonth)
        {
            throw new InvalidOperationException(
                "Bảng lương xem trước không khớp với kỳ đang chọn.");
        }

        Mode =
            PayrollWorkspaceMode.Preview;

        PayrollPeriodId =
            null;

        TimesheetPeriodId =
            preview.TimesheetPeriodId;

        ClosedAtUtc =
            null;

        ClosedByUsername =
            null;

        PreviewIsFinalizable =
            preview.IsFinalizable;

        Rows =
            preview.Employees
                .OrderBy(
                    employee =>
                        employee.EmployeeCode,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    employee =>
                        employee.EmployeeFullName,
                    StringComparer.OrdinalIgnoreCase)
                .Select(
                    employee =>
                        new PayrollWorkspaceRowViewModel(
                            employee.EmployeeId,
                            employee.EmployeeCode,
                            employee.EmployeeFullName,
                            employee.CurrencyCode,
                            employee.BaseSalaryAmount,
                            employee.ApprovedOvertimeMinutes,
                            employee.PayableOvertimeMinutes,
                            employee.OvertimeAmount,
                            employee.GrossAmount,
                            employee.IsFinalizable,
                            employee.IsFinalizable
                                ? "Sẵn sàng"
                                : "Cần xử lý",
                            JoinIssues(
                                employee.Issues)))
                .ToArray();

        EmployeeCount =
            Rows.Count;

        IssueMessages =
            preview.Issues
                .Select(
                    issue =>
                        issue.Message)
                .Concat(
                    preview.Employees
                        .SelectMany(
                            employee =>
                                employee.Issues)
                        .Select(
                            issue =>
                                issue.Message))
                .Where(
                    message =>
                        !string.IsNullOrWhiteSpace(
                            message))
                .Distinct(
                    StringComparer.Ordinal)
                .ToArray();

        CurrencySummaries =
            BuildPreviewSummaries(
                Rows);

        NotifyModeState();
    }

    private void LoadClosedPayroll(
        ClosedPayrollReadModel closedPayroll)
    {
        if (closedPayroll.Year !=
                SelectedYear
            || closedPayroll.Month !=
                SelectedMonth)
        {
            throw new InvalidOperationException(
                "Snapshot kỳ lương đã đóng không khớp với kỳ đang chọn.");
        }

        Mode =
            PayrollWorkspaceMode.Closed;

        PayrollPeriodId =
            closedPayroll.PayrollPeriodId;

        TimesheetPeriodId =
            closedPayroll.TimesheetPeriodId;

        ClosedAtUtc =
            closedPayroll.ClosedAtUtc;

        ClosedByUsername =
            closedPayroll.ClosedByUsername;

        PreviewIsFinalizable =
            false;

        Rows =
            closedPayroll.Employees
                .Select(
                    employee =>
                        new PayrollWorkspaceRowViewModel(
                            employee.EmployeeId,
                            employee.EmployeeCode,
                            employee.EmployeeFullName,
                            employee.CurrencyCode,
                            employee.BaseSalaryAmount,
                            employee.ApprovedOvertimeMinutes,
                            employee.PayableOvertimeMinutes,
                            employee.OvertimeAmount,
                            employee.GrossAmount,
                            true,
                            "Đã khóa",
                            null))
                .ToArray();

        EmployeeCount =
            Rows.Count;

        IssueMessages =
            [];

        CurrencySummaries =
            closedPayroll.CurrencySummaries
                .Select(
                    summary =>
                        new PayrollCurrencySummaryViewModel(
                            summary.CurrencyCode,
                            summary.EmployeeCount,
                            summary.BaseSalaryAmount,
                            summary.OvertimeAmount,
                            summary.GrossAmount,
                            true))
                .ToArray();

        NotifyModeState();
    }

    private static IReadOnlyList<PayrollCurrencySummaryViewModel>
        BuildPreviewSummaries(
            IReadOnlyList<PayrollWorkspaceRowViewModel> rows)
    {
        return rows
            .GroupBy(
                row =>
                    row.CurrencyCode,
                StringComparer.Ordinal)
            .OrderBy(
                group =>
                    group.Key,
                StringComparer.Ordinal)
            .Select(
                group =>
                {
                    PayrollWorkspaceRowViewModel[] items =
                        group.ToArray();

                    bool isComplete =
                        items.All(
                            item =>
                                item.OvertimeAmount.HasValue
                                && item.GrossAmount.HasValue);

                    decimal? overtimeAmount =
                        isComplete
                            ? items.Sum(
                                item =>
                                    item.OvertimeAmount!.Value)
                            : null;

                    decimal? grossAmount =
                        isComplete
                            ? items.Sum(
                                item =>
                                    item.GrossAmount!.Value)
                            : null;

                    return new PayrollCurrencySummaryViewModel(
                        group.Key,
                        items.Length,
                        items.Sum(
                            item =>
                                item.BaseSalaryAmount),
                        overtimeAmount,
                        grossAmount,
                        isComplete);
                })
            .ToArray();
    }

    private bool CanClosePayroll()
    {
        return !IsLoading
            && !IsClosing
            && IsPreview
            && PreviewIsFinalizable;
    }

    private bool ValidatePeriod()
    {
        if (SelectedYear is < 2000 or > 9999)
        {
            ErrorMessage =
                "Năm kỳ lương không hợp lệ.";

            return false;
        }

        if (SelectedMonth is < 1 or > 12)
        {
            ErrorMessage =
                "Tháng kỳ lương phải từ 1 đến 12.";

            return false;
        }

        return true;
    }

    private void ClearWorkspace()
    {
        Mode =
            PayrollWorkspaceMode.Preview;

        Rows =
            [];

        CurrencySummaries =
            [];

        IssueMessages =
            [];

        EmployeeCount =
            0;

        PreviewIsFinalizable =
            false;

        PayrollPeriodId =
            null;

        TimesheetPeriodId =
            null;

        ClosedAtUtc =
            null;

        ClosedByUsername =
            null;

        NotifyModeState();
    }

    private void NotifyModeState()
    {
        OnPropertyChanged(
            nameof(IsPreview));

        OnPropertyChanged(
            nameof(IsClosed));

        OnPropertyChanged(
            nameof(CanClose));

        OnPropertyChanged(
            nameof(ModeText));

        OnPropertyChanged(
            nameof(ClosedInfoText));

        ClosePayrollCommand
            .NotifyCanExecuteChanged();
    }

    private void NotifyCommandState()
    {
        OnPropertyChanged(
            nameof(CanClose));

        ClosePayrollCommand
            .NotifyCanExecuteChanged();
    }

    private static string? JoinIssues(
        IReadOnlyList<PayrollCalculationIssue> issues)
    {
        if (issues.Count == 0)
        {
            return null;
        }

        return string.Join(
            Environment.NewLine,
            issues
                .Select(
                    issue =>
                        issue.Message)
                .Distinct(
                    StringComparer.Ordinal));
    }

    private static string CleanArgumentMessage(
        ArgumentException exception)
    {
        if (!string.IsNullOrWhiteSpace(
                exception.ParamName))
        {
            int markerIndex =
                exception.Message.IndexOf(
                    " (Parameter '",
                    StringComparison.Ordinal);

            if (markerIndex >= 0)
            {
                return exception.Message[
                    ..markerIndex];
            }
        }

        return exception.Message;
    }
}
