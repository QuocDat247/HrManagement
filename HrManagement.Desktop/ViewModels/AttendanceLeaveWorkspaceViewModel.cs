using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;
using HrManagement.Application.Attendance.Corrections;
using HrManagement.Domain.Attendance.Corrections;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Leave.Requests;
using HrManagement.Application.Workspaces.AttendanceLeave;
using HrManagement.Domain.Leave.Requests;
using HrManagement.Application.Attendance.Calculations;
using HrManagement.Application.Attendance.Records;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Application.Attendance.Generation;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class AttendanceLeaveWorkspaceViewModel
    : ObservableObject
{
    partial void OnSelectedLeaveRequestItemChanged(
    LeaveWorkspaceItem? value)
    {
        ApproveLeaveRequestCommand
            .NotifyCanExecuteChanged();

        RejectLeaveRequestCommand
            .NotifyCanExecuteChanged();

        CancelLeaveRequestCommand
            .NotifyCanExecuteChanged();
    }

    private bool CanApproveLeaveRequest()
    {
        return SelectedLeaveRequestItem?
            .Status ==
            LeaveRequestStatus.Pending;
    }

    private bool CanRejectLeaveRequest()
    {
        return SelectedLeaveRequestItem?
            .Status ==
            LeaveRequestStatus.Pending;
    }

    private bool CanCancelLeaveRequest()
    {
        return SelectedLeaveRequestItem?
            .Status is
                LeaveRequestStatus.Pending
                or LeaveRequestStatus.Approved;
    }

    private Task ApproveLeaveRequestAsync()
    {
        return ChangeSelectedLeaveRequestStatusAsync(
            LeaveRequestStatus.Approved);
    }

    private Task RejectLeaveRequestAsync()
    {
        return ChangeSelectedLeaveRequestStatusAsync(
            LeaveRequestStatus.Rejected);
    }

    private Task CancelLeaveRequestAsync()
    {
        return ChangeSelectedLeaveRequestStatusAsync(
            LeaveRequestStatus.Cancelled);
    }

    private async Task ChangeSelectedLeaveRequestStatusAsync(
        LeaveRequestStatus targetStatus)
    {
        LeaveWorkspaceItem? selected =
            SelectedLeaveRequestItem;

        if (selected is null)
        {
            ErrorMessage =
                "Vui lòng chọn đơn nghỉ phép.";

            return;
        }

        ErrorMessage =
            null;

        OperationMessage =
            null;

        try
        {
            IsLoading =
                true;

            ChangeLeaveRequestStatusResult result =
                await _leaveRequestStatusService
                    .ChangeStatusAsync(
                        new ChangeLeaveRequestStatusRequest(
                            selected.LeaveRequestId,
                            targetStatus,
                            StatusChangeNote));

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể cập nhật trạng thái đơn nghỉ phép.";

                return;
            }

            OperationMessage =
                targetStatus switch
                {
                    LeaveRequestStatus.Approved =>
                        "Đã duyệt đơn nghỉ phép.",

                    LeaveRequestStatus.Rejected =>
                        "Đã từ chối đơn nghỉ phép.",

                    LeaveRequestStatus.Cancelled =>
                        "Đã hủy đơn nghỉ phép.",

                    _ =>
                        "Đã cập nhật đơn nghỉ phép."
                };

            StatusChangeNote =
                null;

            await LoadAsync();
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể cập nhật trạng thái đơn nghỉ phép.";
        }
        finally
        {
            IsLoading =
                false;
        }
    }

    private static readonly AttendanceLeaveEmployeeFilterOption
        AllEmployeesOption =
            new(
                null,
                "Tất cả nhân viên");

    private readonly IDailyAttendanceGenerationService
        _dailyAttendanceGenerationService;

    private readonly IAttendancePunchService
        _attendancePunchService;

    private readonly IAttendanceRecalculationService
        _attendanceRecalculationService;

    private readonly IAttendanceCorrectionService
        _attendanceCorrectionService;

    private readonly IAttendanceCorrectionWorkspaceQueryService
        _attendanceCorrectionWorkspaceQueryService;

    private readonly ILeaveRequestStatusService
        _leaveRequestStatusService;

    private readonly IAttendanceLeaveWorkspaceQueryService
        _queryService;

    private readonly ILeaveRequestSubmissionService
        _leaveRequestSubmissionService;

    private readonly TimeProvider
        _timeProvider;

    [ObservableProperty]
    private IReadOnlyList<AttendanceWorkspaceItem>
        attendanceItems =
            Array.Empty<AttendanceWorkspaceItem>();

    [ObservableProperty]
    private IReadOnlyList<LeaveWorkspaceItem>
        leaveRequestItems =
            Array.Empty<LeaveWorkspaceItem>();

    [ObservableProperty]
    private IReadOnlyList<AttendanceLeaveEmployeeFilterOption>
        employeeOptions =
            Array.Empty<AttendanceLeaveEmployeeFilterOption>();

    [ObservableProperty]
    private AttendanceLeaveEmployeeFilterOption?
        selectedEmployeeOption;

    [ObservableProperty]
    private IReadOnlyList<AttendanceLeaveEmployeeItem>
        requestEmployeeOptions =
            Array.Empty<AttendanceLeaveEmployeeItem>();

    [ObservableProperty]
    private AttendanceLeaveEmployeeItem?
        selectedRequestEmployee;

    [ObservableProperty]
    private IReadOnlyList<LeaveTypeWorkspaceOption>
        activeLeaveTypes =
            Array.Empty<LeaveTypeWorkspaceOption>();

    [ObservableProperty]
    private LeaveTypeWorkspaceOption?
        selectedLeaveType;

    [ObservableProperty]
    private DateTime? fromDate;

    [ObservableProperty]
    private DateTime? toDate;

    [ObservableProperty]
    private DateTime? requestStartDate;

    [ObservableProperty]
    private DateTime? requestEndDate;

    [ObservableProperty]
    private string? requestReason;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string? operationMessage;

    [ObservableProperty]
    private LeaveWorkspaceItem?
        selectedLeaveRequestItem;

    [ObservableProperty]
    private string?
        statusChangeNote;

    [ObservableProperty]
    private string? employeeSearchText;

    [ObservableProperty]
    private IReadOnlyList<AttendanceLeaveEmployeeFilterOption>
        filteredEmployeeOptions =
            Array.Empty<AttendanceLeaveEmployeeFilterOption>();

    [ObservableProperty]
    private DateTime? generationDate;

    [ObservableProperty]
    private AttendanceWorkspaceItem?
        selectedAttendanceItem;

    [ObservableProperty]
    private AttendanceCorrectionWorkspaceSnapshot?
        correctionWorkspace;

    [ObservableProperty]
    private IReadOnlyList<AttendanceCorrectionWorkspaceEventItem>
        correctionEventItems =
            Array.Empty<AttendanceCorrectionWorkspaceEventItem>();

    [ObservableProperty]
    private IReadOnlyList<AttendanceCorrectionWorkspaceHistoryItem>
        correctionHistoryItems =
            Array.Empty<AttendanceCorrectionWorkspaceHistoryItem>();

    [ObservableProperty]
    private AttendanceCorrectionWorkspaceEventItem?
        selectedCorrectionEvent;

    [ObservableProperty]
    private AttendanceEventType correctionEventType =
        AttendanceEventType.ClockIn;

    [ObservableProperty]
    private DateTime? correctionDate;

    [ObservableProperty]
    private string? correctionTimeText;

    [ObservableProperty]
    private string? correctionReason;

    [ObservableProperty]
    private bool isCorrectionLoading;

    public IReadOnlyList<AttendanceEventType>
        CorrectionEventTypes
        { get; } =
            Enum.GetValues<AttendanceEventType>();

    public AttendanceLeaveWorkspaceViewModel(
        IAttendanceLeaveWorkspaceQueryService queryService,
        IAttendancePunchService attendancePunchService,
        IAttendanceRecalculationService attendanceRecalculationService,
        ILeaveRequestSubmissionService leaveRequestSubmissionService,
        ILeaveRequestStatusService leaveRequestStatusService,
        IDailyAttendanceGenerationService dailyAttendanceGenerationService,
        IAttendanceCorrectionService attendanceCorrectionService,
        IAttendanceCorrectionWorkspaceQueryService attendanceCorrectionWorkspaceQueryService,
        TimeProvider timeProvider)
    {
        GenerateAttendanceCommand =
            new AsyncRelayCommand(
                GenerateAttendanceAsync,
                CanGenerateAttendance);

        _dailyAttendanceGenerationService =
            dailyAttendanceGenerationService;

        _attendancePunchService =
            attendancePunchService;

        _attendanceRecalculationService =
            attendanceRecalculationService;

        _attendanceCorrectionService =
            attendanceCorrectionService;

        _attendanceCorrectionWorkspaceQueryService =
            attendanceCorrectionWorkspaceQueryService;

        _queryService =
            queryService;

        _leaveRequestSubmissionService =
            leaveRequestSubmissionService;

        _leaveRequestStatusService =
            leaveRequestStatusService;

        _timeProvider =
            timeProvider;

        EmployeeOptions =
        [
            AllEmployeesOption
        ];

        SelectedEmployeeOption =
            AllEmployeesOption;

        FilteredEmployeeOptions =
            EmployeeOptions;

        SetCurrentMonth();

        SetRequestDatesToToday();

        SetGenerationDateToToday();

        LoadCommand =
            new AsyncRelayCommand(
                LoadAsync);

        ClearFiltersCommand =
            new AsyncRelayCommand(
                ClearFiltersAsync);

        SubmitLeaveRequestCommand =
            new AsyncRelayCommand(
                SubmitLeaveRequestAsync);

        ApproveLeaveRequestCommand =
            new AsyncRelayCommand(
                ApproveLeaveRequestAsync,
                CanApproveLeaveRequest);

        RejectLeaveRequestCommand =
            new AsyncRelayCommand(
                RejectLeaveRequestAsync,
                CanRejectLeaveRequest);

        CancelLeaveRequestCommand =
            new AsyncRelayCommand(
                CancelLeaveRequestAsync,
                CanCancelLeaveRequest);

        ClockInCommand =
            new AsyncRelayCommand(
                ClockInAsync,
                CanRecordAttendancePunch);

        ClockOutCommand =
            new AsyncRelayCommand(
                ClockOutAsync,
                CanRecordAttendancePunch);

        LoadCorrectionWorkspaceCommand =
            new AsyncRelayCommand(
        LoadCorrectionWorkspaceAsync);

        AddAttendanceCorrectionCommand =
            new AsyncRelayCommand(
                AddAttendanceCorrectionAsync);

        ChangeAttendanceCorrectionCommand =
            new AsyncRelayCommand(
                ChangeAttendanceCorrectionAsync);

        VoidAttendanceCorrectionCommand =
            new AsyncRelayCommand(
                VoidAttendanceCorrectionAsync);
    }

    public string Title =>
        "Chấm công & Nghỉ phép";

    public IAsyncRelayCommand ClockInCommand
    {
        get;
    }

    public IAsyncRelayCommand ClockOutCommand
    {
        get;
    }

    public IAsyncRelayCommand ApproveLeaveRequestCommand
    {
        get;
    }

    public IAsyncRelayCommand RejectLeaveRequestCommand
    {
        get;
    }

    public IAsyncRelayCommand CancelLeaveRequestCommand
    {
        get;
    }

    public IAsyncRelayCommand LoadCommand
    {
        get;
    }

    public IAsyncRelayCommand ClearFiltersCommand
    {
        get;
    }

    public IAsyncRelayCommand SubmitLeaveRequestCommand
    {
        get;
    }

    public IAsyncRelayCommand GenerateAttendanceCommand
    {
        get;
    }

    public IAsyncRelayCommand LoadCorrectionWorkspaceCommand
    {
        get;
    }

    public IAsyncRelayCommand AddAttendanceCorrectionCommand
    {
        get;
    }

    public IAsyncRelayCommand ChangeAttendanceCorrectionCommand
    {
        get;
    }

    public IAsyncRelayCommand VoidAttendanceCorrectionCommand
    {
        get;
    }

    public async Task LoadAsync()
    {
        if (!FromDate.HasValue)
        {
            ErrorMessage =
                "Vui lòng chọn ngày bắt đầu.";

            return;
        }

        if (!ToDate.HasValue)
        {
            ErrorMessage =
                "Vui lòng chọn ngày kết thúc.";

            return;
        }

        DateOnly fromDate =
            DateOnly.FromDateTime(
                FromDate.Value);

        DateOnly toDate =
            DateOnly.FromDateTime(
                ToDate.Value);

        if (toDate <
            fromDate)
        {
            ErrorMessage =
                "Ngày kết thúc không thể trước ngày bắt đầu.";

            return;
        }

        try
        {
            IsLoading =
                true;

            ErrorMessage =
                null;

            Guid? previouslySelectedAttendanceRecordId =
                SelectedAttendanceItem?
                    .AttendanceRecordId;

            Guid? previouslySelectedLeaveRequestId =
                SelectedLeaveRequestItem?
                    .LeaveRequestId;

            Guid? previouslySelectedFilterEmployeeId =
                SelectedEmployeeOption?
                    .EmployeeId;

            Guid? previouslySelectedRequestEmployeeId =
                SelectedRequestEmployee?
                    .EmployeeId;

            Guid? previouslySelectedLeaveTypeId =
                SelectedLeaveType?
                    .LeaveTypeId;

            IReadOnlyList<AttendanceLeaveEmployeeItem>
                employees =
                    await _queryService
                        .GetEmployeesAsync();

            IReadOnlyList<LeaveTypeWorkspaceOption>
                leaveTypes =
                    await _queryService
                        .GetActiveLeaveTypesAsync();

            RequestEmployeeOptions =
                employees;

            ActiveLeaveTypes =
                leaveTypes;

            var filterOptions =
                new List<
                    AttendanceLeaveEmployeeFilterOption>
                {
                    AllEmployeesOption
                };

            filterOptions.AddRange(
                employees.Select(
                    employee =>
                        new AttendanceLeaveEmployeeFilterOption(
                            employee.EmployeeId,
                            $"{employee.EmployeeCode} - {employee.EmployeeName}")));

            EmployeeOptions =
                filterOptions;

            ApplyEmployeeSearchFilter();

            SelectedEmployeeOption =
                previouslySelectedFilterEmployeeId.HasValue
                    ? FilteredEmployeeOptions.FirstOrDefault(
                        option =>
                            option.EmployeeId ==
                            previouslySelectedFilterEmployeeId)
                        ?? AllEmployeesOption
                    : AllEmployeesOption;

            SelectedRequestEmployee =
                previouslySelectedRequestEmployeeId.HasValue
                    ? employees.FirstOrDefault(
                        employee =>
                            employee.EmployeeId ==
                            previouslySelectedRequestEmployeeId)
                    : employees.FirstOrDefault();

            SelectedLeaveType =
                previouslySelectedLeaveTypeId.HasValue
                    ? leaveTypes.FirstOrDefault(
                        leaveType =>
                            leaveType.LeaveTypeId ==
                            previouslySelectedLeaveTypeId)
                    : leaveTypes.FirstOrDefault();

            AttendanceLeaveWorkspaceSnapshot snapshot =
                await _queryService
                    .GetAsync(
                        new AttendanceLeaveWorkspaceQuery(
                            fromDate,
                            toDate,
                            SelectedEmployeeOption
                                .EmployeeId));

            AttendanceItems =
                snapshot.Attendance;

            SelectedAttendanceItem =
                previouslySelectedAttendanceRecordId.HasValue
                    ? AttendanceItems.FirstOrDefault(
                        item =>
                            item.AttendanceRecordId ==
                            previouslySelectedAttendanceRecordId.Value)
                    : null;

            LeaveRequestItems =
                snapshot.LeaveRequests;

            SelectedLeaveRequestItem =
                previouslySelectedLeaveRequestId.HasValue
                    ? LeaveRequestItems.FirstOrDefault(
                        item =>
                            item.LeaveRequestId ==
                            previouslySelectedLeaveRequestId.Value)
                    : null;

        }
        catch (Exception)
        {
            AttendanceItems =
                Array.Empty<AttendanceWorkspaceItem>();

            LeaveRequestItems =
                Array.Empty<LeaveWorkspaceItem>();

            ErrorMessage =
                "Không thể tải dữ liệu chấm công và nghỉ phép.";
        }
        finally
        {
            IsLoading =
                false;
        }
    }

    public async Task ClearFiltersAsync()
    {
        EmployeeSearchText =
    null;

        SetCurrentMonth();

        SelectedEmployeeOption =
            EmployeeOptions.FirstOrDefault(
                option =>
                    !option.EmployeeId.HasValue)
            ?? AllEmployeesOption;

        await LoadAsync();
    }

    private async Task SubmitLeaveRequestAsync()
    {
        ErrorMessage =
            null;

        OperationMessage =
            null;

        if (SelectedRequestEmployee is null)
        {
            ErrorMessage =
                "Vui lòng chọn nhân viên.";

            return;
        }

        if (SelectedLeaveType is null)
        {
            ErrorMessage =
                "Vui lòng chọn loại nghỉ phép.";

            return;
        }

        if (!RequestStartDate.HasValue)
        {
            ErrorMessage =
                "Vui lòng chọn ngày bắt đầu nghỉ.";

            return;
        }

        if (!RequestEndDate.HasValue)
        {
            ErrorMessage =
                "Vui lòng chọn ngày kết thúc nghỉ.";

            return;
        }

        DateOnly startDate =
            DateOnly.FromDateTime(
                RequestStartDate.Value);

        DateOnly endDate =
            DateOnly.FromDateTime(
                RequestEndDate.Value);

        if (endDate <
            startDate)
        {
            ErrorMessage =
                "Ngày kết thúc nghỉ không thể trước ngày bắt đầu nghỉ.";

            return;
        }

        try
        {
            IsLoading =
                true;

            SubmitLeaveRequestResult result =
                await _leaveRequestSubmissionService
                    .SubmitAsync(
                        new SubmitLeaveRequestRequest(
                            SelectedRequestEmployee.EmployeeId,
                            SelectedLeaveType.LeaveTypeId,
                            startDate,
                            endDate,
                            RequestReason));

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể gửi đơn nghỉ phép.";

                return;
            }

            OperationMessage =
                "Đã gửi đơn nghỉ phép.";

            RequestReason =
                null;

            SetRequestDatesToToday();

            await LoadAsync();
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể gửi đơn nghỉ phép.";
        }
        finally
        {
            IsLoading =
                false;
        }
    }

    private void SetCurrentMonth()
    {
        DateTime localToday =
            GetLocalToday();

        DateTime firstDay =
            new(
                localToday.Year,
                localToday.Month,
                1);

        DateTime lastDay =
            firstDay
                .AddMonths(1)
                .AddDays(-1);

        FromDate =
            firstDay;

        ToDate =
            lastDay;
    }

    private void SetRequestDatesToToday()
    {
        DateTime localToday =
            GetLocalToday();

        RequestStartDate =
            localToday;

        RequestEndDate =
            localToday;
    }

    private DateTime GetLocalToday()
    {
        return _timeProvider
            .GetLocalNow()
            .Date;
    }

    partial void OnEmployeeSearchTextChanged(
    string? value)
    {
        ApplyEmployeeSearchFilter();
    }

    private void ApplyEmployeeSearchFilter()
    {
        string normalizedSearch =
            EmployeeSearchText?
                .Trim()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(
                normalizedSearch))
        {
            FilteredEmployeeOptions =
                EmployeeOptions;

            return;
        }

        FilteredEmployeeOptions =
            EmployeeOptions
                .Where(
                    option =>
                        !option.EmployeeId.HasValue
                        || option.DisplayName.Contains(
                            normalizedSearch,
                            StringComparison.OrdinalIgnoreCase))
                .ToArray();

        if (SelectedEmployeeOption is not null
            && !FilteredEmployeeOptions.Contains(
                SelectedEmployeeOption))
        {
            SelectedEmployeeOption =
                AllEmployeesOption;
        }
    }

    partial void OnSelectedAttendanceItemChanged(
        AttendanceWorkspaceItem? value)
    {
        SelectedCorrectionEvent =
            null;

        CorrectionWorkspace =
            null;

        CorrectionEventItems =
            Array.Empty<AttendanceCorrectionWorkspaceEventItem>();

        CorrectionHistoryItems =
            Array.Empty<AttendanceCorrectionWorkspaceHistoryItem>();

        CorrectionReason =
            null;

        CorrectionTimeText =
            null;

        if (value is null)
        {
            CorrectionDate =
                null;

            return;
        }

        CorrectionDate =
            value.WorkDate
                .ToDateTime(
                    TimeOnly.MinValue);

        if (!IsLoading)
        {
            LoadCorrectionWorkspaceCommand
                .Execute(
                    null);
        }
    }

    partial void OnSelectedCorrectionEventChanged(
        AttendanceCorrectionWorkspaceEventItem? value)
    {
        if (value is null)
        {
            return;
        }

        CorrectionEventType =
            value.EventType;

        CorrectionDate =
            value.OccurredAtLocal.Date;

        CorrectionTimeText =
            value.OccurredAtLocal
                .ToString(
                    "HH:mm",
                    CultureInfo.InvariantCulture);
    }

    partial void OnSelectedEmployeeOptionChanged(
    AttendanceLeaveEmployeeFilterOption? value)
    {
        ClockInCommand?
            .NotifyCanExecuteChanged();

        ClockOutCommand?
            .NotifyCanExecuteChanged();
    }

    private bool CanRecordAttendancePunch()
    {
        return SelectedEmployeeOption?
            .EmployeeId
            .HasValue ==
            true;
    }

    private Task ClockInAsync()
    {
        return RecordAttendancePunchAsync(
            AttendanceEventType.ClockIn);
    }

    private Task ClockOutAsync()
    {
        return RecordAttendancePunchAsync(
            AttendanceEventType.ClockOut);
    }

    private async Task RecordAttendancePunchAsync(
        AttendanceEventType eventType)
    {
        Guid? employeeId =
            SelectedEmployeeOption?
                .EmployeeId;

        if (!employeeId.HasValue)
        {
            ErrorMessage =
                "Vui lòng chọn một nhân viên để chấm công.";

            return;
        }

        ErrorMessage =
            null;

        OperationMessage =
            null;

        try
        {
            IsLoading =
                true;

            DateTime occurredAtUtc =
                _timeProvider
                    .GetUtcNow()
                    .UtcDateTime;

            RecordAttendancePunchResult punchResult =
                await _attendancePunchService
                    .RecordAsync(
                        new RecordAttendancePunchRequest(
                            employeeId.Value,
                            eventType,
                            occurredAtUtc));

            if (!punchResult.IsSuccessful)
            {
                ErrorMessage =
                    punchResult.ErrorMessage
                    ?? "Không thể ghi nhận chấm công.";

                return;
            }

            string successMessage =
                eventType ==
                    AttendanceEventType.ClockIn
                    ? "Đã ghi nhận vào ca."
                    : "Đã ghi nhận ra ca.";

            string? recalculationError =
                null;

            if (punchResult.AttendanceRecordId.HasValue)
            {
                RecalculateAttendanceResult recalculation =
                    await _attendanceRecalculationService
                        .RecalculateAsync(
                            new RecalculateAttendanceRequest(
                                punchResult
                                    .AttendanceRecordId
                                    .Value));

                if (!recalculation.IsSuccessful)
                {
                    recalculationError =
                        recalculation.ErrorMessage
                        ?? "Không thể tính lại dữ liệu chấm công.";
                }
            }

            await LoadAsync();

            OperationMessage =
                successMessage;

            if (recalculationError is not null)
            {
                ErrorMessage =
                    $"Đã ghi nhận chấm công nhưng chưa thể tính lại: {recalculationError}";
            }
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể ghi nhận chấm công.";
        }
        finally
        {
            IsLoading =
                false;
        }
    }

    private bool CanGenerateAttendance()
    {
        return !IsLoading
            && GenerationDate.HasValue;
    }

    private async Task GenerateAttendanceAsync()
    {
        if (!GenerationDate.HasValue)
        {
            ErrorMessage =
                "Vui lòng chọn ngày sinh dữ liệu chấm công.";

            return;
        }

        ErrorMessage =
            null;

        OperationMessage =
            null;

        DateOnly workDate =
            DateOnly.FromDateTime(
                GenerationDate.Value);

        Guid? employeeId =
            SelectedEmployeeOption?
                .EmployeeId;

        try
        {
            IsLoading =
                true;

            GenerateDailyAttendanceResult result =
                await _dailyAttendanceGenerationService
                    .GenerateAsync(
                        new GenerateDailyAttendanceRequest(
                            workDate,
                            employeeId));

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể sinh dữ liệu chấm công.";

                return;
            }

            string message;

            if (result.CreatedCount == 0
                && result.SkippedExistingCount == 0)
            {
                message =
                    "Không có phân lịch làm việc phù hợp "
                    + "để sinh dữ liệu chấm công cho ngày đã chọn.";
            }
            else if (result.CreatedCount == 0)
            {
                message =
                    $"Không có bản ghi mới. Đã bỏ qua "
                    + $"{result.SkippedExistingCount} "
                    + "bản ghi đã tồn tại.";
            }
            else if (result.SkippedExistingCount == 0)
            {
                message =
                    $"Đã sinh {result.CreatedCount} "
                    + "bản ghi chấm công.";
            }
            else
            {
                message =
                    $"Đã sinh {result.CreatedCount} "
                    + "bản ghi chấm công; bỏ qua "
                    + $"{result.SkippedExistingCount} "
                    + "bản ghi đã tồn tại.";
            }

            await LoadAsync();

            OperationMessage =
                message;
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể sinh dữ liệu chấm công.";
        }
        finally
        {
            IsLoading =
                false;
        }
    }

    private async Task LoadCorrectionWorkspaceAsync()
    {
        AttendanceWorkspaceItem? selected =
            SelectedAttendanceItem;

        if (selected is null)
        {
            CorrectionWorkspace =
                null;

            CorrectionEventItems =
                Array.Empty<AttendanceCorrectionWorkspaceEventItem>();

            CorrectionHistoryItems =
                Array.Empty<AttendanceCorrectionWorkspaceHistoryItem>();

            return;
        }

        Guid? previouslySelectedEventId =
            SelectedCorrectionEvent?
                .EventId;

        try
        {
            IsCorrectionLoading =
                true;

            AttendanceCorrectionWorkspaceSnapshot? snapshot =
                await _attendanceCorrectionWorkspaceQueryService
                    .GetAsync(
                        selected.AttendanceRecordId);

            if (snapshot is null)
            {
                CorrectionWorkspace =
                    null;

                CorrectionEventItems =
                    Array.Empty<AttendanceCorrectionWorkspaceEventItem>();

                CorrectionHistoryItems =
                    Array.Empty<AttendanceCorrectionWorkspaceHistoryItem>();

                ErrorMessage =
                    "Không tìm thấy bản ghi chấm công cần điều chỉnh.";

                return;
            }

            CorrectionWorkspace =
                snapshot;

            CorrectionEventItems =
                snapshot.EffectiveEvents;

            CorrectionHistoryItems =
                snapshot.Corrections;

            SelectedCorrectionEvent =
                previouslySelectedEventId.HasValue
                    ? CorrectionEventItems.FirstOrDefault(
                        item =>
                            item.EventId ==
                            previouslySelectedEventId.Value)
                    : null;
        }
        catch (Exception)
        {
            CorrectionWorkspace =
                null;

            CorrectionEventItems =
                Array.Empty<AttendanceCorrectionWorkspaceEventItem>();

            CorrectionHistoryItems =
                Array.Empty<AttendanceCorrectionWorkspaceHistoryItem>();

            ErrorMessage =
                "Không thể tải dữ liệu điều chỉnh chấm công.";
        }
        finally
        {
            IsCorrectionLoading =
                false;
        }
    }

    private Task AddAttendanceCorrectionAsync()
    {
        return ApplyAttendanceCorrectionAsync(
            AttendanceCorrectionKind.AddEvent);
    }

    private Task ChangeAttendanceCorrectionAsync()
    {
        return ApplyAttendanceCorrectionAsync(
            AttendanceCorrectionKind.ChangeEvent);
    }

    private Task VoidAttendanceCorrectionAsync()
    {
        return ApplyAttendanceCorrectionAsync(
            AttendanceCorrectionKind.VoidEvent);
    }

    private async Task ApplyAttendanceCorrectionAsync(
        AttendanceCorrectionKind kind)
    {
        AttendanceWorkspaceItem? attendance =
            SelectedAttendanceItem;

        if (attendance is null)
        {
            ErrorMessage =
                "Vui lòng chọn một bản ghi chấm công.";

            return;
        }

        if (string.IsNullOrWhiteSpace(
                CorrectionReason))
        {
            ErrorMessage =
                "Vui lòng nhập lý do điều chỉnh.";

            return;
        }

        Guid? affectedEventId =
            kind ==
                AttendanceCorrectionKind.AddEvent
                ? null
                : SelectedCorrectionEvent?
                    .EventId;

        if (kind !=
                AttendanceCorrectionKind.AddEvent
            && !affectedEventId.HasValue)
        {
            ErrorMessage =
                "Vui lòng chọn sự kiện chấm công cần điều chỉnh.";

            return;
        }

        AttendanceEventType? afterEventType =
            null;

        DateTime? afterOccurredAtUtc =
            null;

        if (kind !=
            AttendanceCorrectionKind.VoidEvent)
        {
            if (!TryGetCorrectionOccurredAtUtc(
                    out DateTime occurredAtUtc,
                    out string? validationError))
            {
                ErrorMessage =
                    validationError;

                return;
            }

            afterEventType =
                CorrectionEventType;

            afterOccurredAtUtc =
                occurredAtUtc;
        }

        ErrorMessage =
            null;

        OperationMessage =
            null;

        try
        {
            IsCorrectionLoading =
                true;

            ApplyAttendanceCorrectionResult result =
                await _attendanceCorrectionService
                    .ApplyAsync(
                        new ApplyAttendanceCorrectionRequest(
                            attendance.AttendanceRecordId,
                            kind,
                            affectedEventId,
                            afterEventType,
                            afterOccurredAtUtc,
                            CorrectionReason.Trim()));

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể áp dụng điều chỉnh chấm công.";

                return;
            }

            RecalculateAttendanceResult recalculation =
                await _attendanceRecalculationService
                    .RecalculateAsync(
                        new RecalculateAttendanceRequest(
                            attendance.AttendanceRecordId));

            string successMessage =
                kind switch
                {
                    AttendanceCorrectionKind.AddEvent =>
                        "Đã bổ sung sự kiện chấm công.",

                    AttendanceCorrectionKind.ChangeEvent =>
                        "Đã sửa sự kiện chấm công.",

                    AttendanceCorrectionKind.VoidEvent =>
                        "Đã hủy hiệu lực sự kiện chấm công.",

                    _ =>
                        "Đã điều chỉnh chấm công."
                };

            CorrectionReason =
                null;

            await LoadAsync();

            await LoadCorrectionWorkspaceAsync();

            OperationMessage =
                successMessage;

            if (!recalculation.IsSuccessful)
            {
                ErrorMessage =
                    "Điều chỉnh đã được lưu nhưng chưa thể tính lại chấm công: "
                    + (
                        recalculation.ErrorMessage
                        ?? "Không xác định được lỗi.");
            }
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể áp dụng điều chỉnh chấm công.";
        }
        finally
        {
            IsCorrectionLoading =
                false;
        }
    }

    private bool TryGetCorrectionOccurredAtUtc(
    out DateTime occurredAtUtc,
    out string? errorMessage)
    {
        occurredAtUtc =
            default;

        errorMessage =
            null;

        if (CorrectionWorkspace is null)
        {
            errorMessage =
                "Chưa tải dữ liệu bản ghi chấm công.";

            return false;
        }

        if (!CorrectionDate.HasValue)
        {
            errorMessage =
                "Vui lòng chọn ngày của sự kiện.";

            return false;
        }

        string timeText =
            CorrectionTimeText?
                .Trim()
            ?? string.Empty;

        if (!TimeOnly.TryParseExact(
                timeText,
                [
                    "H:mm",
                "HH:mm"
                ],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out TimeOnly localTime))
        {
            errorMessage =
                "Giờ chấm công phải có định dạng HH:mm.";

            return false;
        }

        DateTime date =
            CorrectionDate.Value;

        DateTime localDateTime =
            new(
                date.Year,
                date.Month,
                date.Day,
                localTime.Hour,
                localTime.Minute,
                0,
                DateTimeKind.Unspecified);

        TimeZoneInfo timeZone;

        try
        {
            timeZone =
                TimeZoneInfo.FindSystemTimeZoneById(
                    CorrectionWorkspace.TimeZoneId);
        }
        catch (
            TimeZoneNotFoundException)
        {
            errorMessage =
                "Không tìm thấy múi giờ của bản ghi chấm công.";

            return false;
        }
        catch (
            InvalidTimeZoneException)
        {
            errorMessage =
                "Múi giờ của bản ghi chấm công không hợp lệ.";

            return false;
        }

        if (timeZone.IsInvalidTime(
                localDateTime))
        {
            errorMessage =
                "Thời điểm đã chọn không tồn tại trong múi giờ của bản ghi.";

            return false;
        }

        if (timeZone.IsAmbiguousTime(
                localDateTime))
        {
            errorMessage =
                "Thời điểm đã chọn bị trùng do chuyển đổi múi giờ.";

            return false;
        }

        occurredAtUtc =
            TimeZoneInfo.ConvertTimeToUtc(
                localDateTime,
                timeZone);

        return true;
    }

    private void SetGenerationDateToToday()
    {
        GenerationDate =
            GetLocalToday();
    }

    partial void OnGenerationDateChanged(
    DateTime? value)
    {
        GenerateAttendanceCommand?
            .NotifyCanExecuteChanged();
    }

    partial void OnIsLoadingChanged(
        bool value)
    {
        GenerateAttendanceCommand?
            .NotifyCanExecuteChanged();
    }
}
