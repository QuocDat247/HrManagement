using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Desktop.Services;
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

    private readonly IOvertimeRequestStatusService
        _statusService;

    private readonly IUserConfirmationService
        _confirmationService;

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
    private string transitionApprovedMinutesText =
        string.Empty;

    [ObservableProperty]
    private string? transitionNote;

    [ObservableProperty]
    private bool isChangingStatus;

    [ObservableProperty]
    private string? transitionErrorMessage;

    [ObservableProperty]
    private string? transitionSuccessMessage;

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
        IOvertimeRequestStatusService statusService,
        IUserConfirmationService confirmationService,
        TimeProvider timeProvider)
    {
        _queryService =
            queryService;

        _submitService =
            submitService;

        _statusService =
            statusService;

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

        ApproveCommand =
            new AsyncRelayCommand(
                ApproveSelectedAsync,
                CanApproveSelected);

        RejectCommand =
            new AsyncRelayCommand(
                RejectSelectedAsync,
                CanRejectSelected);

        CancelCommand =
            new AsyncRelayCommand(
                CancelSelectedAsync,
                CanCancelSelected);
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

    public IAsyncRelayCommand ApproveCommand
    {
        get;
    }

    public IAsyncRelayCommand RejectCommand
    {
        get;
    }

    public IAsyncRelayCommand CancelCommand
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
        TransitionErrorMessage =
            null;

        TransitionSuccessMessage =
            null;

        TransitionNote =
            null;

        TransitionApprovedMinutesText =
            value?.Status ==
                OvertimeRequestStatus.Pending
                ? value.RequestedMinutes.ToString()
                : value?.ApprovedMinutes?.ToString()
                    ?? string.Empty;

        ApproveCommand?
            .NotifyCanExecuteChanged();

        RejectCommand?
            .NotifyCanExecuteChanged();

        CancelCommand?
            .NotifyCanExecuteChanged();

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

    private bool CanApproveSelected()
    {
        return
            !IsChangingStatus
            && SelectedRow?.Status ==
                OvertimeRequestStatus.Pending;
    }

    private bool CanRejectSelected()
    {
        return
            !IsChangingStatus
            && SelectedRow?.Status ==
                OvertimeRequestStatus.Pending;
    }

    private bool CanCancelSelected()
    {
        return
            !IsChangingStatus
            && SelectedRow?.Status is
                OvertimeRequestStatus.Pending
                or OvertimeRequestStatus.Approved;
    }

    private Task ApproveSelectedAsync()
    {
        return ChangeSelectedStatusAsync(
            OvertimeRequestStatus.Approved);
    }

    private Task RejectSelectedAsync()
    {
        return ChangeSelectedStatusAsync(
            OvertimeRequestStatus.Rejected);
    }

    private Task CancelSelectedAsync()
    {
        return ChangeSelectedStatusAsync(
            OvertimeRequestStatus.Cancelled);
    }

    private async Task ChangeSelectedStatusAsync(
        OvertimeRequestStatus targetStatus)
    {
        TransitionErrorMessage =
            null;

        TransitionSuccessMessage =
            null;

        OvertimeWorkspaceRowViewModel? row =
            SelectedRow;

        if (row is null)
        {
            TransitionErrorMessage =
                "Vui lòng chọn một yêu cầu tăng ca.";

            return;
        }

        int? approvedMinutes =
            null;

        if (targetStatus ==
            OvertimeRequestStatus.Approved)
        {
            if (!int.TryParse(
                    TransitionApprovedMinutesText,
                    out int parsedApprovedMinutes)
                || parsedApprovedMinutes <= 0
                || parsedApprovedMinutes >
                    row.RequestedMinutes)
            {
                TransitionErrorMessage =
                    "Số phút tăng ca được duyệt phải từ 1 đến số phút đã yêu cầu.";

                return;
            }

            approvedMinutes =
                parsedApprovedMinutes;
        }

        string? normalizedNote =
            string.IsNullOrWhiteSpace(
                TransitionNote)
                ? null
                : TransitionNote.Trim();

        if (normalizedNote?.Length > 500)
        {
            TransitionErrorMessage =
                "Ghi chú thay đổi trạng thái không được vượt quá 500 ký tự.";

            return;
        }

        bool confirmed =
            _confirmationService.Confirm(
                GetConfirmationTitle(
                    targetStatus),
                GetConfirmationMessage(
                    row,
                    targetStatus,
                    approvedMinutes));

        if (!confirmed)
        {
            return;
        }

        Guid overtimeRequestId =
            row.OvertimeRequestId;

        IsChangingStatus =
            true;

        try
        {
            ChangeOvertimeRequestStatusResult result =
                await _statusService
                    .ChangeStatusAsync(
                        new ChangeOvertimeRequestStatusRequest(
                            overtimeRequestId,
                            row.Status,
                            targetStatus,
                            approvedMinutes,
                            normalizedNote));

            if (!result.IsSuccessful)
            {
                TransitionErrorMessage =
                    string.IsNullOrWhiteSpace(
                        result.ErrorMessage)
                        ? "Không thể thay đổi trạng thái yêu cầu tăng ca."
                        : result.ErrorMessage;

                return;
            }

            if (SelectedStatusOption?.Status.HasValue ==
                true)
            {
                SelectedStatusOption =
                    StatusOptions.First(
                        option =>
                            !option.Status.HasValue);
            }

            await LoadAsync();

            SelectedRow =
                Rows.FirstOrDefault(
                    current =>
                        current.OvertimeRequestId ==
                        overtimeRequestId);

            TransitionNote =
                null;

            TransitionSuccessMessage =
                GetSuccessMessage(
                    targetStatus);
        }
        catch (Exception exception)
        {
            TransitionErrorMessage =
                exception.Message;
        }
        finally
        {
            IsChangingStatus =
                false;
        }
    }

    private static string GetConfirmationTitle(
        OvertimeRequestStatus targetStatus)
    {
        return targetStatus switch
        {
            OvertimeRequestStatus.Approved =>
                "Xác nhận duyệt tăng ca",

            OvertimeRequestStatus.Rejected =>
                "Xác nhận từ chối tăng ca",

            OvertimeRequestStatus.Cancelled =>
                "Xác nhận hủy yêu cầu tăng ca",

            _ =>
                "Xác nhận thay đổi tăng ca"
        };
    }

    private static string GetConfirmationMessage(
        OvertimeWorkspaceRowViewModel row,
        OvertimeRequestStatus targetStatus,
        int? approvedMinutes)
    {
        return targetStatus switch
        {
            OvertimeRequestStatus.Approved =>
                $"Duyệt {approvedMinutes} phút tăng ca cho "
                + $"{row.EmployeeCode} — {row.EmployeeName} "
                + $"ngày {row.WorkDateText}?",

            OvertimeRequestStatus.Rejected =>
                $"Từ chối yêu cầu tăng ca của "
                + $"{row.EmployeeCode} — {row.EmployeeName} "
                + $"ngày {row.WorkDateText}?",

            OvertimeRequestStatus.Cancelled =>
                $"Hủy yêu cầu tăng ca của "
                + $"{row.EmployeeCode} — {row.EmployeeName} "
                + $"ngày {row.WorkDateText}?",

            _ =>
                "Bạn có chắc chắn muốn tiếp tục?"
        };
    }

    private static string GetSuccessMessage(
        OvertimeRequestStatus targetStatus)
    {
        return targetStatus switch
        {
            OvertimeRequestStatus.Approved =>
                "Đã duyệt yêu cầu tăng ca thành công.",

            OvertimeRequestStatus.Rejected =>
                "Đã từ chối yêu cầu tăng ca.",

            OvertimeRequestStatus.Cancelled =>
                "Đã hủy yêu cầu tăng ca.",

            _ =>
                "Đã cập nhật yêu cầu tăng ca."
        };
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

    partial void OnIsChangingStatusChanged(
    bool value)
    {
        ApproveCommand?
            .NotifyCanExecuteChanged();

        RejectCommand?
            .NotifyCanExecuteChanged();

        CancelCommand?
            .NotifyCanExecuteChanged();
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
