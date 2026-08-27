using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Overtime.Requests;
using HrManagement.Application.Workspaces.Overtime;
using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class OvertimeWorkspaceViewModel
    : ObservableObject
{
    private readonly IOvertimeWorkspaceQueryService
        _queryService;

    private readonly ISubmitOvertimeRequestService
        _submitService;

    private readonly TimeProvider
        _timeProvider;

    private int _historyLoadVersion;

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
    private IReadOnlyList<OvertimeEmployeeFilterOption>
        employeeOptions =
            [];

    [ObservableProperty]
    private OvertimeEmployeeFilterOption?
        selectedEmployeeOption;

    [ObservableProperty]
    private IReadOnlyList<OvertimeStatusFilterOption>
        statusOptions =
            [];

    [ObservableProperty]
    private OvertimeStatusFilterOption?
        selectedStatusOption;

    [ObservableProperty]
    private IReadOnlyList<OvertimeWorkspaceRowViewModel>
        rows =
            [];

    [ObservableProperty]
    private OvertimeWorkspaceRowViewModel?
        selectedRow;

    [ObservableProperty]
    private IReadOnlyList<OvertimeStatusHistoryRowViewModel>
        historyRows =
            [];

    [ObservableProperty]
    private int requestCount;

    [ObservableProperty]
    private int pendingCount;

    [ObservableProperty]
    private int approvedCount;

    [ObservableProperty]
    private int rejectedCount;

    [ObservableProperty]
    private int cancelledCount;

    [ObservableProperty]
    private int totalApprovedMinutes;

    [ObservableProperty]
    private IReadOnlyList<OvertimeSubmissionEmployeeOption>
        submissionEmployeeOptions =
            [];

    [ObservableProperty]
    private OvertimeSubmissionEmployeeOption?
        selectedSubmissionEmployeeOption;

    [ObservableProperty]
    private DateTime? submissionWorkDate;

    [ObservableProperty]
    private string submissionRequestedMinutesText =
        "120";

    [ObservableProperty]
    private string? submissionReason;

    [ObservableProperty]
    private bool isSubmitting;

    [ObservableProperty]
    private string? submissionErrorMessage;

    [ObservableProperty]
    private string? submissionSuccessMessage;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isHistoryLoading;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string? historyErrorMessage;

    public OvertimeWorkspaceViewModel(
        IOvertimeWorkspaceQueryService queryService,
        ISubmitOvertimeRequestService submitService,
        TimeProvider timeProvider)
    {
        _queryService =
            queryService;

        _submitService =
            submitService;

        _timeProvider =
            timeProvider;

        DateTimeOffset localNow =
            _timeProvider.GetLocalNow();

        SelectedYear =
            localNow.Year;

        SelectedMonth =
            localNow.Month;

        SubmissionWorkDate =
            localNow.Date;

        YearOptions =
            CreateYearOptions(
                localNow.Year);

        MonthOptions =
            Enumerable
                .Range(
                    1,
                    12)
                .ToArray();

        EmployeeOptions =
        [
            new OvertimeEmployeeFilterOption(
                null,
                "Tất cả nhân viên")
        ];

        SelectedEmployeeOption =
            EmployeeOptions[0];

        StatusOptions =
        [
            new OvertimeStatusFilterOption(
                null,
                "Tất cả trạng thái"),

            new OvertimeStatusFilterOption(
                OvertimeRequestStatus.Pending,
                "Chờ duyệt"),

            new OvertimeStatusFilterOption(
                OvertimeRequestStatus.Approved,
                "Đã duyệt"),

            new OvertimeStatusFilterOption(
                OvertimeRequestStatus.Rejected,
                "Từ chối"),

            new OvertimeStatusFilterOption(
                OvertimeRequestStatus.Cancelled,
                "Đã hủy")
        ];

        SelectedStatusOption =
            StatusOptions[0];

        LoadCommand =
            new AsyncRelayCommand(
                LoadAsync);

        RefreshCommand =
            LoadCommand;

        RefreshHistoryCommand =
            new AsyncRelayCommand(
                RefreshSelectedHistoryAsync,
                CanRefreshHistory);

        SubmitCommand =
            new AsyncRelayCommand(
                SubmitAsync,
                CanSubmit);
    }

    public IAsyncRelayCommand LoadCommand
    {
        get;
    }

    public IAsyncRelayCommand RefreshCommand
    {
        get;
    }

    public IAsyncRelayCommand RefreshHistoryCommand
    {
        get;
    }

    public IAsyncRelayCommand SubmitCommand
    {
        get;
    }

    private async Task LoadAsync()
    {
        ErrorMessage =
            null;

        if (SelectedYear is < 2000 or > 9999)
        {
            ClearRows();

            ErrorMessage =
                "Năm tra cứu tăng ca không hợp lệ.";

            return;
        }

        if (SelectedMonth is < 1 or > 12)
        {
            ClearRows();

            ErrorMessage =
                "Tháng tra cứu tăng ca phải từ 1 đến 12.";

            return;
        }

        IsLoading =
            true;

        try
        {
            await LoadEmployeeOptionsAsync();

            OvertimeWorkspaceSnapshot snapshot =
                await _queryService
                    .GetAsync(
                        new OvertimeWorkspaceQuery(
                            SelectedYear,
                            SelectedMonth,
                            SelectedEmployeeOption?
                                .EmployeeId,
                            SelectedStatusOption?
                                .Status));

            ApplySnapshot(
                snapshot);
        }
        catch (Exception exception)
        {
            ClearRows();

            ErrorMessage =
                exception.Message;
        }
        finally
        {
            IsLoading =
                false;
        }
    }

    private async Task LoadEmployeeOptionsAsync()
    {
        Guid? selectedEmployeeId =
            SelectedEmployeeOption?
                .EmployeeId;

        Guid? selectedSubmissionEmployeeId =
            SelectedSubmissionEmployeeOption?
                .EmployeeId;

        IReadOnlyList<OvertimeEmployeeOption>
            employees =
                await _queryService
                    .GetEmployeesAsync();

        OvertimeSubmissionEmployeeOption[]
            submissionOptions =
                employees
                    .Select(
                        employee =>
                            new OvertimeSubmissionEmployeeOption(
                                employee.EmployeeId,
                                $"{employee.EmployeeCode} — {employee.EmployeeName}"))
                    .ToArray();

                SubmissionEmployeeOptions =
                    submissionOptions;

                SelectedSubmissionEmployeeOption =
                    submissionOptions.FirstOrDefault(
                        option =>
                            option.EmployeeId ==
                            selectedSubmissionEmployeeId)
                    ?? submissionOptions.FirstOrDefault();

        OvertimeEmployeeFilterOption[] options =
        [
            new(
                null,
                "Tất cả nhân viên"),

            .. employees.Select(
                employee =>
                    new OvertimeEmployeeFilterOption(
                        employee.EmployeeId,
                        $"{employee.EmployeeCode} — {employee.EmployeeName}"))
        ];

        EmployeeOptions =
            options;

        SelectedEmployeeOption =
            options.FirstOrDefault(
                option =>
                    option.EmployeeId ==
                    selectedEmployeeId)
            ?? options[0];
    }

    private void ApplySnapshot(
        OvertimeWorkspaceSnapshot snapshot)
    {
        SelectedRow =
            null;

        OvertimeWorkspaceRowViewModel[] loadedRows =
            snapshot
                .Requests
                .Select(
                    OvertimeWorkspaceRowViewModel.From)
                .ToArray();

        Rows =
            loadedRows;

        RequestCount =
            loadedRows.Length;

        PendingCount =
            loadedRows.Count(
                row =>
                    row.Status ==
                    OvertimeRequestStatus.Pending);

        ApprovedCount =
            loadedRows.Count(
                row =>
                    row.Status ==
                    OvertimeRequestStatus.Approved);

        RejectedCount =
            loadedRows.Count(
                row =>
                    row.Status ==
                    OvertimeRequestStatus.Rejected);

        CancelledCount =
            loadedRows.Count(
                row =>
                    row.Status ==
                    OvertimeRequestStatus.Cancelled);

        TotalApprovedMinutes =
            loadedRows.Sum(
                row =>
                    row.ApprovedMinutes
                    ?? 0);
    }

    private void ClearRows()
    {
        SelectedRow =
            null;

        Rows =
            [];

        HistoryRows =
            [];

        HistoryErrorMessage =
            null;

        RequestCount =
            0;

        PendingCount =
            0;

        ApprovedCount =
            0;

        RejectedCount =
            0;

        CancelledCount =
            0;

        TotalApprovedMinutes =
            0;
    }

    partial void OnSelectedRowChanged(
        OvertimeWorkspaceRowViewModel? value)
    {
        int loadVersion =
            ++_historyLoadVersion;

        HistoryRows =
            [];

        HistoryErrorMessage =
            null;

        RefreshHistoryCommand?
            .NotifyCanExecuteChanged();

        if (value is null)
        {
            IsHistoryLoading =
                false;

            return;
        }

        _ = LoadHistoryAsync(
            value.OvertimeRequestId,
            loadVersion);
    }

    private bool CanRefreshHistory()
    {
        return
            SelectedRow is not null
            && !IsHistoryLoading;
    }

    private async Task RefreshSelectedHistoryAsync()
    {
        OvertimeWorkspaceRowViewModel? row =
            SelectedRow;

        if (row is null)
        {
            return;
        }

        int loadVersion =
            ++_historyLoadVersion;

        await LoadHistoryAsync(
            row.OvertimeRequestId,
            loadVersion);
    }

    private async Task LoadHistoryAsync(
        Guid overtimeRequestId,
        int loadVersion)
    {
        if (loadVersion !=
            _historyLoadVersion)
        {
            return;
        }

        IsHistoryLoading =
            true;

        RefreshHistoryCommand
            .NotifyCanExecuteChanged();

        try
        {
            IReadOnlyList<OvertimeStatusHistoryItem>
                history =
                    await _queryService
                        .GetHistoryAsync(
                            overtimeRequestId);

            if (loadVersion !=
                _historyLoadVersion)
            {
                return;
            }

            HistoryRows =
                history
                    .Select(
                        item =>
                            OvertimeStatusHistoryRowViewModel.From(
                                item,
                                _timeProvider.LocalTimeZone))
                    .ToArray();
        }
        catch (Exception exception)
        {
            if (loadVersion ==
                _historyLoadVersion)
            {
                HistoryRows =
                    [];

                HistoryErrorMessage =
                    exception.Message;
            }
        }
        finally
        {
            if (loadVersion ==
                _historyLoadVersion)
            {
                IsHistoryLoading =
                    false;

                RefreshHistoryCommand
                    .NotifyCanExecuteChanged();
            }
        }
    }

    private bool CanSubmit()
    {
        return
            !IsSubmitting
            && SelectedSubmissionEmployeeOption is not null
            && SubmissionWorkDate.HasValue;
    }

    private async Task SubmitAsync()
    {
        SubmissionErrorMessage =
            null;

        SubmissionSuccessMessage =
            null;

        OvertimeSubmissionEmployeeOption? employee =
            SelectedSubmissionEmployeeOption;

        if (employee is null)
        {
            SubmissionErrorMessage =
                "Vui lòng chọn nhân viên.";

            return;
        }

        if (!SubmissionWorkDate.HasValue)
        {
            SubmissionErrorMessage =
                "Vui lòng chọn ngày tăng ca.";

            return;
        }

        if (!int.TryParse(
                SubmissionRequestedMinutesText,
                out int requestedMinutes)
            || requestedMinutes is <= 0 or > 1440)
        {
            SubmissionErrorMessage =
                "Số phút tăng ca yêu cầu phải từ 1 đến 1440 phút.";

            return;
        }

        string? normalizedReason =
            string.IsNullOrWhiteSpace(
                SubmissionReason)
                ? null
                : SubmissionReason.Trim();

        if (normalizedReason?.Length > 500)
        {
            SubmissionErrorMessage =
                "Lý do tăng ca không được vượt quá 500 ký tự.";

            return;
        }

        DateOnly workDate =
            DateOnly.FromDateTime(
                SubmissionWorkDate.Value);

        IsSubmitting =
            true;

        SubmitCommand
            .NotifyCanExecuteChanged();

        try
        {
            SubmitOvertimeRequestResult result =
                await _submitService
                    .SubmitAsync(
                        new SubmitOvertimeRequestRequest(
                            employee.EmployeeId,
                            workDate,
                            requestedMinutes,
                            normalizedReason));

            if (!result.IsSuccessful)
            {
                SubmissionErrorMessage =
                    string.IsNullOrWhiteSpace(
                        result.ErrorMessage)
                        ? "Không thể gửi yêu cầu tăng ca."
                        : result.ErrorMessage;

                return;
            }

            SubmissionSuccessMessage =
                "Đã gửi yêu cầu tăng ca thành công.";

            SubmissionRequestedMinutesText =
                "120";

            SubmissionReason =
                null;

            SelectedYear =
                workDate.Year;

            SelectedMonth =
                workDate.Month;

            SelectedStatusOption =
                StatusOptions.First(
                    option =>
                        !option.Status.HasValue);

            SelectedEmployeeOption =
                EmployeeOptions.FirstOrDefault(
                    option =>
                        option.EmployeeId ==
                        employee.EmployeeId)
                ?? EmployeeOptions.FirstOrDefault();

            await LoadAsync();

            if (result.OvertimeRequestId.HasValue)
            {
                SelectedRow =
                    Rows.FirstOrDefault(
                        row =>
                            row.OvertimeRequestId ==
                            result.OvertimeRequestId.Value);
            }
        }
        catch (Exception exception)
        {
            SubmissionErrorMessage =
                exception.Message;
        }
        finally
        {
            IsSubmitting =
                false;

            SubmitCommand
                .NotifyCanExecuteChanged();
        }
    }

    partial void OnSelectedSubmissionEmployeeOptionChanged(
        OvertimeSubmissionEmployeeOption? value)
    {
        SubmitCommand?
            .NotifyCanExecuteChanged();
    }

    partial void OnSubmissionWorkDateChanged(
        DateTime? value)
    {
        SubmitCommand?
            .NotifyCanExecuteChanged();
    }

    partial void OnIsSubmittingChanged(
        bool value)
    {
        SubmitCommand?
            .NotifyCanExecuteChanged();
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
