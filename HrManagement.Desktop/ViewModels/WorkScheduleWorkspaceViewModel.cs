using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Attendance.Schedules;
using HrManagement.Application.Workspaces.WorkSchedules;
using System.Globalization;
using HrManagement.Desktop.Services;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class WorkScheduleWorkspaceViewModel
    : ObservableObject
{
    private static readonly WorkScheduleEmployeeFilterOption
        AllEmployeesOption =
            new(
                null,
                "Tất cả nhân viên");

    private readonly IUserConfirmationService
        _confirmationService;

    private readonly IWorkScheduleDayManagementService
        _dayManagementService;

    private readonly IWorkScheduleManagementService
        _scheduleManagementService;

    private readonly IWorkScheduleWorkspaceQueryService
        _queryService;

    private readonly IEmployeeWorkScheduleAssignmentService
        _assignmentService;

    private readonly TimeProvider
        _timeProvider;

    private IReadOnlyList<WorkScheduleWorkspaceDayItem>
        _lastScheduleDays =
            [];

    [ObservableProperty]
    private IReadOnlyList<WorkScheduleEmployeeFilterOption>
        employeeOptions =
            [AllEmployeesOption];

    [ObservableProperty]
    private IReadOnlyList<WorkScheduleEmployeeFilterOption>
        filteredEmployeeOptions =
            [AllEmployeesOption];

    [ObservableProperty]
    private WorkScheduleEmployeeFilterOption?
        selectedEmployeeOption =
            AllEmployeesOption;

    [ObservableProperty]
    private string?
        employeeSearchText;

    [ObservableProperty]
    private IReadOnlyList<WorkScheduleWorkspaceScheduleItem>
        scheduleItems =
            [];

    [ObservableProperty]
    private IReadOnlyList<WorkScheduleWorkspaceScheduleItem>
        activeScheduleItems =
            [];

    [ObservableProperty]
    private WorkScheduleWorkspaceScheduleItem?
        selectedScheduleItem;

    [ObservableProperty]
    private WorkScheduleWorkspaceScheduleItem?
        selectedAssignmentScheduleItem;

    [ObservableProperty]
    private IReadOnlyList<WorkScheduleWorkspaceDayItem>
        selectedScheduleDays =
            [];

    [ObservableProperty]
    private IReadOnlyList<WorkScheduleWorkspaceAssignmentItem>
        assignmentItems =
            [];

    [ObservableProperty]
    private DateTime?
        assignmentEffectiveFrom;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string?
        operationMessage;

    [ObservableProperty]
    private string?
        errorMessage;

    [ObservableProperty]
    private bool isCreatingSchedule;

    [ObservableProperty]
    private string? scheduleEditorCode;

    [ObservableProperty]
    private string? scheduleEditorName;

    [ObservableProperty]
    private string? scheduleEditorTimeZoneId =
        "SE Asia Standard Time";

    [ObservableProperty]
    private WorkScheduleWorkspaceDayItem?
    selectedScheduleDayItem;

    [ObservableProperty]
    private bool dayEditorIsWorkingDay;

    [ObservableProperty]
    private string? dayEditorStartTimeText;

    [ObservableProperty]
    private string? dayEditorEndTimeText;

    [ObservableProperty]
    private string dayEditorBreakMinutesText =
        "0";

    private Guid? _cloneSourceScheduleId;

    [ObservableProperty]
    private bool isCloningSchedule;

    public bool IsScheduleTimeZoneEditorEnabled =>
        !IsCloningSchedule;

    private bool _isLoadingDayEditor;

    public WorkScheduleWorkspaceViewModel(
    IWorkScheduleWorkspaceQueryService queryService,
    IEmployeeWorkScheduleAssignmentService assignmentService,
    IWorkScheduleManagementService scheduleManagementService,
    IWorkScheduleDayManagementService dayManagementService,
    IUserConfirmationService confirmationService,
    TimeProvider timeProvider)
    {
        _confirmationService =
            confirmationService;

        _queryService =
            queryService;

        _assignmentService =
            assignmentService;

        _scheduleManagementService =
            scheduleManagementService;

        _dayManagementService =
            dayManagementService;

        _timeProvider =
            timeProvider;

        AssignmentEffectiveFrom =
            _timeProvider
                .GetLocalNow()
                .Date;

        LoadCommand =
            new AsyncRelayCommand(
                LoadAsync);

        ClearFiltersCommand =
            new AsyncRelayCommand(
                ClearFiltersAsync);

        AssignScheduleCommand =
            new AsyncRelayCommand(
                AssignScheduleAsync,
                CanAssignSchedule);

        NewScheduleCommand =
            new RelayCommand(
                BeginCreateSchedule,
                () => !IsLoading);

        CloneScheduleCommand =
            new RelayCommand(
                BeginCloneSchedule,
                CanCloneSchedule);

        SaveScheduleCommand =
            new AsyncRelayCommand(
                SaveScheduleAsync,
                CanSaveSchedule);

        DeactivateScheduleCommand =
            new AsyncRelayCommand(
                DeactivateScheduleAsync,
                CanDeactivateSchedule);

        ReactivateScheduleCommand =
            new AsyncRelayCommand(
                ReactivateScheduleAsync,
                CanReactivateSchedule);

        SaveScheduleDayCommand =
            new AsyncRelayCommand(
                SaveScheduleDayAsync,
                CanSaveScheduleDay);

        DeleteScheduleCommand =
            new AsyncRelayCommand(
                DeleteScheduleAsync,
                CanDeleteSchedule);
    }

    public IAsyncRelayCommand DeleteScheduleCommand
    {
        get;
    }

    public IRelayCommand CloneScheduleCommand
    {
        get;
    }

    public IAsyncRelayCommand SaveScheduleDayCommand
    {
        get;
    }

    public IRelayCommand NewScheduleCommand
    {
        get;
    }

    public IAsyncRelayCommand SaveScheduleCommand
    {
        get;
    }

    public IAsyncRelayCommand DeactivateScheduleCommand
    {
        get;
    }

    public IAsyncRelayCommand ReactivateScheduleCommand
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

    public IAsyncRelayCommand AssignScheduleCommand
    {
        get;
    }

    public async Task LoadAsync()
    {
        ErrorMessage =
            null;

        OperationMessage =
            null;

        try
        {
            IsLoading =
                true;

            DayOfWeek? previouslySelectedDayOfWeek =
                SelectedScheduleDayItem?
                    .DayOfWeek;

            Guid? previouslySelectedEmployeeId =
                SelectedEmployeeOption?
                    .EmployeeId;

            Guid? previouslySelectedScheduleId =
                SelectedScheduleItem?
                    .WorkScheduleId;

            Guid? previouslySelectedAssignmentScheduleId =
                SelectedAssignmentScheduleItem?
                    .WorkScheduleId;

            IReadOnlyList<WorkScheduleWorkspaceEmployeeItem>
                employees =
                    await _queryService
                        .GetEmployeesAsync();

            EmployeeOptions =
            [
                AllEmployeesOption,

                .. employees.Select(
                    employee =>
                        new WorkScheduleEmployeeFilterOption(
                            employee.EmployeeId,
                            $"{employee.EmployeeCode} - {employee.EmployeeName}"))
            ];

            ApplyEmployeeSearchFilter();

            SelectedEmployeeOption =
                previouslySelectedEmployeeId.HasValue
                    ? FilteredEmployeeOptions
                        .FirstOrDefault(
                            option =>
                                option.EmployeeId ==
                                previouslySelectedEmployeeId)
                        ?? AllEmployeesOption
                    : AllEmployeesOption;

            WorkScheduleWorkspaceSnapshot snapshot =
                await _queryService
                    .GetAsync(
                        new WorkScheduleWorkspaceQuery(
                            SelectedEmployeeOption?
                                .EmployeeId));

            ScheduleItems =
                snapshot.Schedules;

            ActiveScheduleItems =
                snapshot.Schedules
                    .Where(
                        schedule =>
                            schedule.IsActive)
                    .OrderBy(
                        schedule =>
                            schedule.Code)
                    .ThenBy(
                        schedule =>
                            schedule.Name)
                    .ThenBy(
                        schedule =>
                            schedule.WorkScheduleId)
                    .ToArray();

            AssignmentItems =
                snapshot.Assignments;

            SelectedScheduleItem =
                previouslySelectedScheduleId.HasValue
                    ? ScheduleItems.FirstOrDefault(
                        schedule =>
                            schedule.WorkScheduleId ==
                            previouslySelectedScheduleId)
                        ?? ScheduleItems.FirstOrDefault()
                    : ScheduleItems.FirstOrDefault();

            SelectedAssignmentScheduleItem =
                previouslySelectedAssignmentScheduleId.HasValue
                    ? ActiveScheduleItems.FirstOrDefault(
                        schedule =>
                            schedule.WorkScheduleId ==
                            previouslySelectedAssignmentScheduleId)
                        ?? ActiveScheduleItems.FirstOrDefault()
                    : ActiveScheduleItems.FirstOrDefault();

            RefreshSelectedScheduleDays(
                snapshot.ScheduleDays,
                previouslySelectedDayOfWeek);
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể tải dữ liệu lịch làm việc.";
        }
        finally
        {
            IsLoading =
                false;
        }
    }

    private async Task ClearFiltersAsync()
    {
        EmployeeSearchText =
            null;

        SelectedEmployeeOption =
            AllEmployeesOption;

        await LoadAsync();
    }

    private void BeginCreateSchedule()
    {
        ErrorMessage =
            null;

        OperationMessage =
            null;

        _cloneSourceScheduleId =
            null;

        IsCloningSchedule =
            false;

        IsCreatingSchedule =
            true;

        SelectedScheduleItem =
            null;

        ScheduleEditorCode =
            null;

        ScheduleEditorName =
            null;

        ScheduleEditorTimeZoneId =
            "SE Asia Standard Time";
    }

    private bool CanSaveSchedule()
    {
        return !IsLoading
            && (
                IsCreatingSchedule
                || SelectedScheduleItem is not null)
            && !string.IsNullOrWhiteSpace(
                ScheduleEditorCode)
            && !string.IsNullOrWhiteSpace(
                ScheduleEditorName)
            && !string.IsNullOrWhiteSpace(
                ScheduleEditorTimeZoneId);
    }

    private async Task SaveScheduleAsync()
    {
        ErrorMessage =
            null;

        OperationMessage =
            null;

        string code =
            ScheduleEditorCode?
                .Trim()
            ?? string.Empty;

        string name =
            ScheduleEditorName?
                .Trim()
            ?? string.Empty;

        string timeZoneId =
            ScheduleEditorTimeZoneId?
                .Trim()
            ?? string.Empty;

        try
        {
            IsLoading =
                true;

            WorkScheduleManagementResult result;

            if (IsCloningSchedule)
            {
                if (!_cloneSourceScheduleId.HasValue)
                {
                    ErrorMessage =
                        "Không xác định được mẫu lịch nguồn.";

                    return;
                }

                result =
                    await _scheduleManagementService
                        .CloneAsync(
                            new CloneWorkScheduleRequest(
                                _cloneSourceScheduleId.Value,
                                code,
                                name));
            }
            else if (IsCreatingSchedule)
            {
                result =
                    await _scheduleManagementService
                        .CreateAsync(
                            new CreateWorkScheduleRequest(
                                code,
                                name,
                                timeZoneId));
            }
            else
            {
                if (SelectedScheduleItem is null)
                {
                    ErrorMessage =
                        "Vui lòng chọn mẫu lịch làm việc.";

                    return;
                }

                result =
                    await _scheduleManagementService
                        .UpdateAsync(
                            new UpdateWorkScheduleRequest(
                                SelectedScheduleItem.WorkScheduleId,
                                code,
                                name,
                                timeZoneId));
            }

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể lưu mẫu lịch làm việc.";

                return;
            }

            bool wasCreating =
                IsCreatingSchedule;

            bool wasCloning =
                IsCloningSchedule;

            Guid? savedScheduleId =
                result.WorkScheduleId;

            IsCreatingSchedule =
                false;

            IsCloningSchedule =
                false;

            _cloneSourceScheduleId =
                null;

            await LoadAsync();

            if (savedScheduleId.HasValue)
            {
                SelectedScheduleItem =
                    ScheduleItems.FirstOrDefault(
                        schedule =>
                            schedule.WorkScheduleId ==
                            savedScheduleId.Value)
                    ?? SelectedScheduleItem;
            }

            OperationMessage =
                wasCloning
                    ? $"Đã sao chép thành mẫu lịch {code} - {name}. Hãy kiểm tra cấu hình trước khi kích hoạt."
                    : wasCreating
                        ? $"Đã tạo mẫu lịch {code} - {name}. Hãy cấu hình ngày làm việc trước khi kích hoạt."
                        : $"Đã cập nhật mẫu lịch {code} - {name}.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể lưu mẫu lịch làm việc.";
        }
        finally
        {
            IsLoading =
                false;
        }
    }

    private bool CanDeactivateSchedule()
    {
        return !IsLoading
            && !IsCreatingSchedule
            && SelectedScheduleItem?
                .IsActive == true;
    }

    private async Task DeactivateScheduleAsync()
    {
        WorkScheduleWorkspaceScheduleItem? schedule =
            SelectedScheduleItem;

        if (schedule is null)
        {
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

            WorkScheduleManagementResult result =
                await _scheduleManagementService
                    .DeactivateAsync(
                        schedule.WorkScheduleId);

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể ngừng sử dụng mẫu lịch.";

                return;
            }

            await LoadAsync();

            OperationMessage =
                $"Đã ngừng sử dụng mẫu lịch {schedule.Code} - {schedule.Name}.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể ngừng sử dụng mẫu lịch.";
        }
        finally
        {
            IsLoading =
                false;
        }
    }

    private bool CanReactivateSchedule()
    {
        return !IsLoading
            && !IsCreatingSchedule
            && SelectedScheduleItem is
            {
                IsActive: false
            };
    }

    private async Task ReactivateScheduleAsync()
    {
        WorkScheduleWorkspaceScheduleItem? schedule =
            SelectedScheduleItem;

        if (schedule is null)
        {
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

            WorkScheduleManagementResult result =
                await _scheduleManagementService
                    .ReactivateAsync(
                        schedule.WorkScheduleId);

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể kích hoạt mẫu lịch.";

                return;
            }

            await LoadAsync();

            OperationMessage =
                $"Đã kích hoạt mẫu lịch {schedule.Code} - {schedule.Name}.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể kích hoạt mẫu lịch.";
        }
        finally
        {
            IsLoading =
                false;
        }
    }

    private void LoadScheduleEditor(
        WorkScheduleWorkspaceScheduleItem? schedule)
    {
        if (schedule is null)
        {
            ScheduleEditorCode =
                null;

            ScheduleEditorName =
                null;

            ScheduleEditorTimeZoneId =
                "SE Asia Standard Time";

            return;
        }

        ScheduleEditorCode =
            schedule.Code;

        ScheduleEditorName =
            schedule.Name;

        ScheduleEditorTimeZoneId =
            schedule.TimeZoneId;
    }

    private bool CanAssignSchedule()
    {
        return !IsLoading
            && SelectedEmployeeOption?
                .EmployeeId is not null
            && SelectedAssignmentScheduleItem is not null
            && SelectedAssignmentScheduleItem.IsActive
            && AssignmentEffectiveFrom.HasValue;
    }

    private async Task AssignScheduleAsync()
    {
        ErrorMessage =
            null;

        OperationMessage =
            null;

        Guid? employeeId =
            SelectedEmployeeOption?
                .EmployeeId;

        WorkScheduleWorkspaceScheduleItem? schedule =
            SelectedAssignmentScheduleItem;

        DateTime? effectiveFrom =
            AssignmentEffectiveFrom;

        if (!employeeId.HasValue)
        {
            ErrorMessage =
                "Vui lòng chọn một nhân viên để phân lịch.";

            return;
        }

        if (schedule is null)
        {
            ErrorMessage =
                "Vui lòng chọn lịch làm việc.";

            return;
        }

        if (!schedule.IsActive)
        {
            ErrorMessage =
                "Lịch làm việc đã ngừng sử dụng.";

            return;
        }

        if (!effectiveFrom.HasValue)
        {
            ErrorMessage =
                "Vui lòng chọn ngày bắt đầu áp dụng.";

            return;
        }

        string employeeDisplayName =
            SelectedEmployeeOption?
                .DisplayName
            ?? string.Empty;

        string scheduleDisplayName =
            $"{schedule.Code} - {schedule.Name}";

        try
        {
            IsLoading =
                true;

            AssignEmployeeWorkScheduleResult result =
                await _assignmentService
                    .AssignAsync(
                        new AssignEmployeeWorkScheduleRequest(
                            employeeId.Value,
                            schedule.WorkScheduleId,
                            DateOnly.FromDateTime(
                                effectiveFrom.Value)));

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể phân lịch làm việc.";

                return;
            }

            await LoadAsync();

            OperationMessage =
                $"Đã phân {scheduleDisplayName} cho {employeeDisplayName}.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể phân lịch làm việc.";
        }
        finally
        {
            IsLoading =
                false;
        }
    }

    partial void OnEmployeeSearchTextChanged(
        string? value)
    {
        ApplyEmployeeSearchFilter();
    }

    partial void OnSelectedEmployeeOptionChanged(
        WorkScheduleEmployeeFilterOption? value)
    {
        AssignScheduleCommand?
            .NotifyCanExecuteChanged();
    }

    partial void OnSelectedAssignmentScheduleItemChanged(
        WorkScheduleWorkspaceScheduleItem? value)
    {
        AssignScheduleCommand?
            .NotifyCanExecuteChanged();
    }

    partial void OnAssignmentEffectiveFromChanged(
        DateTime? value)
    {
        AssignScheduleCommand?
            .NotifyCanExecuteChanged();
    }

    partial void OnIsLoadingChanged(
    bool value)
    {
        AssignScheduleCommand?
            .NotifyCanExecuteChanged();

        NewScheduleCommand?
            .NotifyCanExecuteChanged();

        SaveScheduleCommand?
            .NotifyCanExecuteChanged();

        DeactivateScheduleCommand?
            .NotifyCanExecuteChanged();

        ReactivateScheduleCommand?
            .NotifyCanExecuteChanged();

        SaveScheduleDayCommand?
            .NotifyCanExecuteChanged();

        CloneScheduleCommand?
            .NotifyCanExecuteChanged();

        DeleteScheduleCommand?
            .NotifyCanExecuteChanged();
    }

    partial void OnSelectedScheduleItemChanged(
    WorkScheduleWorkspaceScheduleItem? value)
    {
        DayOfWeek? preferredDayOfWeek =
            SelectedScheduleDayItem?
                .DayOfWeek;

        if (value is null)
        {
            SelectedScheduleDays =
                [];

            SelectedScheduleDayItem =
                null;

            if (!IsCreatingSchedule)
            {
                LoadScheduleEditor(
                    null);
            }
        }
        else
        {
            IsCreatingSchedule =
                false;

            RefreshSelectedScheduleDaysForCurrentSchedule(
                preferredDayOfWeek);

            LoadScheduleEditor(
                value);
        }

        SaveScheduleCommand?
            .NotifyCanExecuteChanged();

        DeactivateScheduleCommand?
            .NotifyCanExecuteChanged();

        ReactivateScheduleCommand?
            .NotifyCanExecuteChanged();

        SaveScheduleDayCommand?
            .NotifyCanExecuteChanged();

        CloneScheduleCommand?
            .NotifyCanExecuteChanged();

        DeleteScheduleCommand?
            .NotifyCanExecuteChanged();
    }

    partial void OnIsCloningScheduleChanged(
    bool value)
    {
        OnPropertyChanged(
            nameof(IsScheduleTimeZoneEditorEnabled));

        SaveScheduleCommand?
            .NotifyCanExecuteChanged();
    }

    private void RefreshSelectedScheduleDays(
    IReadOnlyList<WorkScheduleWorkspaceDayItem> days,
    DayOfWeek? preferredDayOfWeek = null)
    {
        _lastScheduleDays =
            days;

        RefreshSelectedScheduleDaysForCurrentSchedule(
            preferredDayOfWeek);
    }

    private void RefreshSelectedScheduleDaysForCurrentSchedule(
        DayOfWeek? preferredDayOfWeek = null)
    {
        if (SelectedScheduleItem is null)
        {
            SelectedScheduleDays =
                [];

            SelectedScheduleDayItem =
                null;

            return;
        }

        WorkScheduleWorkspaceDayItem[] visibleDays =
            _lastScheduleDays
                .Where(
                    day =>
                        day.WorkScheduleId ==
                        SelectedScheduleItem.WorkScheduleId)
                .OrderBy(
                    day =>
                        GetDaySortOrder(
                            day.DayOfWeek))
                .ThenBy(
                    day =>
                        day.WorkScheduleDayId)
                .ToArray();

        SelectedScheduleDays =
            visibleDays;

        SelectedScheduleDayItem =
            preferredDayOfWeek.HasValue
                ? visibleDays.FirstOrDefault(
                    day =>
                        day.DayOfWeek ==
                        preferredDayOfWeek.Value)
                    ?? visibleDays.FirstOrDefault()
                : visibleDays.FirstOrDefault();
    }

    private void ApplyEmployeeSearchFilter()
    {
        string search =
            EmployeeSearchText?
                .Trim()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(
                search))
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
                            search,
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

    private static int GetDaySortOrder(
        DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => 1,
            DayOfWeek.Tuesday => 2,
            DayOfWeek.Wednesday => 3,
            DayOfWeek.Thursday => 4,
            DayOfWeek.Friday => 5,
            DayOfWeek.Saturday => 6,
            DayOfWeek.Sunday => 7,
            _ => 8
        };
    }

    partial void OnScheduleEditorCodeChanged(
    string? value)
    {
        SaveScheduleCommand?
            .NotifyCanExecuteChanged();
    }

    partial void OnScheduleEditorNameChanged(
        string? value)
    {
        SaveScheduleCommand?
            .NotifyCanExecuteChanged();
    }

    partial void OnScheduleEditorTimeZoneIdChanged(
        string? value)
    {
        SaveScheduleCommand?
            .NotifyCanExecuteChanged();
    }

    partial void OnIsCreatingScheduleChanged(
        bool value)
    {
        SaveScheduleCommand?
            .NotifyCanExecuteChanged();

        DeactivateScheduleCommand?
            .NotifyCanExecuteChanged();

        ReactivateScheduleCommand?
            .NotifyCanExecuteChanged();

        SaveScheduleDayCommand?
            .NotifyCanExecuteChanged();

        CloneScheduleCommand?
            .NotifyCanExecuteChanged();

        DeleteScheduleCommand?
            .NotifyCanExecuteChanged();
    }

    partial void OnSelectedScheduleDayItemChanged(
    WorkScheduleWorkspaceDayItem? value)
    {
        LoadDayEditor(
            value);

        SaveScheduleDayCommand?
            .NotifyCanExecuteChanged();
    }

    private void LoadDayEditor(
        WorkScheduleWorkspaceDayItem? day)
    {
        _isLoadingDayEditor =
            true;

        try
        {
            if (day is null)
            {
                DayEditorIsWorkingDay =
                    false;

                DayEditorStartTimeText =
                    null;

                DayEditorEndTimeText =
                    null;

                DayEditorBreakMinutesText =
                    "0";

                return;
            }

            DayEditorIsWorkingDay =
                day.IsWorkingDay;

            DayEditorStartTimeText =
                day.StartTime?
                    .ToString(
                        "HH:mm",
                        CultureInfo.InvariantCulture);

            DayEditorEndTimeText =
                day.EndTime?
                    .ToString(
                        "HH:mm",
                        CultureInfo.InvariantCulture);

            DayEditorBreakMinutesText =
                day.BreakMinutes.ToString(
                    CultureInfo.InvariantCulture);
        }
        finally
        {
            _isLoadingDayEditor =
                false;
        }
    }

    partial void OnDayEditorIsWorkingDayChanged(
        bool value)
    {
        if (_isLoadingDayEditor)
        {
            return;
        }

        if (value)
        {
            if (string.IsNullOrWhiteSpace(
                    DayEditorStartTimeText))
            {
                DayEditorStartTimeText =
                    "08:00";
            }

            if (string.IsNullOrWhiteSpace(
                    DayEditorEndTimeText))
            {
                DayEditorEndTimeText =
                    "17:00";
            }

            if (string.IsNullOrWhiteSpace(
                    DayEditorBreakMinutesText)
                || DayEditorBreakMinutesText ==
                    "0")
            {
                DayEditorBreakMinutesText =
                    "60";
            }
        }
        else
        {
            DayEditorStartTimeText =
                null;

            DayEditorEndTimeText =
                null;

            DayEditorBreakMinutesText =
                "0";
        }
    }

    private bool CanSaveScheduleDay()
    {
        return !IsLoading
            && !IsCreatingSchedule
            && SelectedScheduleItem is not null
            && SelectedScheduleDayItem is not null;
    }

    private async Task SaveScheduleDayAsync()
    {
        ErrorMessage =
            null;

        OperationMessage =
            null;

        WorkScheduleWorkspaceScheduleItem? schedule =
            SelectedScheduleItem;

        WorkScheduleWorkspaceDayItem? day =
            SelectedScheduleDayItem;

        if (schedule is null
            || day is null)
        {
            ErrorMessage =
                "Vui lòng chọn mẫu lịch và ngày cần chỉnh sửa.";

            return;
        }

        TimeOnly? startTime =
            null;

        TimeOnly? endTime =
            null;

        int breakMinutes =
            0;

        if (DayEditorIsWorkingDay)
        {
            if (!TryParseTime(
                    DayEditorStartTimeText,
                    out TimeOnly parsedStart))
            {
                ErrorMessage =
                    "Giờ bắt đầu phải có định dạng HH:mm, ví dụ 08:00.";

                return;
            }

            if (!TryParseTime(
                    DayEditorEndTimeText,
                    out TimeOnly parsedEnd))
            {
                ErrorMessage =
                    "Giờ kết thúc phải có định dạng HH:mm, ví dụ 17:00.";

                return;
            }

            if (!int.TryParse(
                    DayEditorBreakMinutesText,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out breakMinutes)
                || breakMinutes < 0)
            {
                ErrorMessage =
                    "Số phút nghỉ phải là số nguyên không âm.";

                return;
            }

            startTime =
                parsedStart;

            endTime =
                parsedEnd;
        }

        try
        {
            IsLoading =
                true;

            WorkScheduleDayManagementResult result =
                await _dayManagementService
                    .UpdateAsync(
                        new UpdateWorkScheduleDayRequest(
                            schedule.WorkScheduleId,
                            day.DayOfWeek,
                            DayEditorIsWorkingDay,
                            startTime,
                            endTime,
                            breakMinutes));

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể cập nhật ngày làm việc.";

                return;
            }

            DayOfWeek savedDay =
                day.DayOfWeek;

            string scheduleDisplayName =
                $"{schedule.Code} - {schedule.Name}";

            await LoadAsync();

            SelectedScheduleDayItem =
                SelectedScheduleDays.FirstOrDefault(
                    item =>
                        item.DayOfWeek ==
                        savedDay);

            OperationMessage =
                $"Đã cập nhật {GetVietnameseDayName(savedDay)} cho mẫu lịch {scheduleDisplayName}.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể cập nhật ngày làm việc.";
        }
        finally
        {
            IsLoading =
                false;
        }
    }

    private static bool TryParseTime(
        string? value,
        out TimeOnly result)
    {
        return TimeOnly.TryParseExact(
            value?.Trim(),
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);
    }

    private static string GetVietnameseDayName(
        DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => "Thứ 2",
            DayOfWeek.Tuesday => "Thứ 3",
            DayOfWeek.Wednesday => "Thứ 4",
            DayOfWeek.Thursday => "Thứ 5",
            DayOfWeek.Friday => "Thứ 6",
            DayOfWeek.Saturday => "Thứ 7",
            DayOfWeek.Sunday => "Chủ nhật",
            _ => dayOfWeek.ToString()
        };
    }

    private bool CanCloneSchedule()
    {
        return !IsLoading
            && !IsCreatingSchedule
            && SelectedScheduleItem is not null;
    }

    private void BeginCloneSchedule()
    {
        WorkScheduleWorkspaceScheduleItem? source =
            SelectedScheduleItem;

        if (source is null)
        {
            return;
        }

        ErrorMessage =
            null;

        OperationMessage =
            null;

        _cloneSourceScheduleId =
            source.WorkScheduleId;

        IsCreatingSchedule =
            true;

        IsCloningSchedule =
            true;

        ScheduleEditorCode =
            $"{source.Code}-COPY";

        ScheduleEditorName =
            $"Bản sao - {source.Name}";

        ScheduleEditorTimeZoneId =
            source.TimeZoneId;

        SelectedScheduleItem =
            null;
    }

    private bool CanDeleteSchedule()
    {
        return !IsLoading
            && !IsCreatingSchedule
            && SelectedScheduleItem is
            {
                IsActive: false
            };
    }

    private async Task DeleteScheduleAsync()
    {
        WorkScheduleWorkspaceScheduleItem? schedule =
            SelectedScheduleItem;

        if (schedule is null)
        {
            return;
        }

        bool confirmed =
            _confirmationService.Confirm(
                "Xóa mẫu lịch làm việc",
                $"Bạn có chắc muốn xóa mẫu "
                + $"{schedule.Code} - {schedule.Name}?\n\n"
                + "Thao tác này chỉ thực hiện được nếu mẫu "
                + "chưa từng được phân lịch hoặc dùng trong chấm công.");

        if (!confirmed)
        {
            return;
        }

        ErrorMessage =
            null;

        OperationMessage =
            null;

        WorkScheduleManagementResult result =
            await _scheduleManagementService
                .DeleteAsync(
                    schedule.WorkScheduleId);

        if (!result.IsSuccessful)
        {
            ErrorMessage =
                result.ErrorMessage
                ?? "Không thể xóa mẫu lịch làm việc.";

            return;
        }

        string deletedCode =
            schedule.Code;

        string deletedName =
            schedule.Name;

        await LoadAsync();

        OperationMessage =
            $"Đã xóa mẫu lịch "
            + $"{deletedCode} - {deletedName}.";
    }
}
