using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Attendance.Calendars;
using HrManagement.Application.Attendance.Schedules.Overrides;
using HrManagement.Application.Workspaces.HolidayExceptions;
using System.Globalization;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class HolidayExceptionWorkspaceViewModel
    : ObservableObject
{
    private readonly IHolidayExceptionWorkspaceQueryService
        _queryService;

    private readonly IHolidayCalendarManagementService
        _holidayManagementService;

    private readonly IWorkScheduleDateOverrideManagementService
        _overrideManagementService;

    private readonly TimeProvider
        _timeProvider;

    [ObservableProperty]
    private IReadOnlyList<int> yearOptions =
        [];

    [ObservableProperty]
    private int selectedYear;

    [ObservableProperty]
    private IReadOnlyList<HolidayExceptionWorkspaceHolidayItem>
        holidayItems =
            [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsEditingExistingHoliday))]
    private HolidayExceptionWorkspaceHolidayItem?
        selectedHolidayItem;

    [ObservableProperty]
    private IReadOnlyList<HolidayExceptionWorkspaceScheduleItem>
        scheduleItems =
            [];

    [ObservableProperty]
    private HolidayExceptionWorkspaceScheduleItem?
        selectedScheduleItem;

    [ObservableProperty]
    private IReadOnlyList<HolidayExceptionWorkspaceOverrideItem>
        overrideItems =
            [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsEditingExistingOverride))]
    private HolidayExceptionWorkspaceOverrideItem?
        selectedOverrideItem;

    [ObservableProperty]
    private DateTime? holidayEditorDate;

    [ObservableProperty]
    private string? holidayEditorName;

    [ObservableProperty]
    private DateTime? overrideEditorWorkDate;

    [ObservableProperty]
    private bool overrideEditorIsWorkingDay =
        true;

    [ObservableProperty]
    private string? overrideEditorStartTimeText =
        "08:00";

    [ObservableProperty]
    private string? overrideEditorEndTimeText =
        "17:00";

    [ObservableProperty]
    private string overrideEditorBreakMinutesText =
        "60";

    [ObservableProperty]
    private string? overrideEditorNote;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? operationMessage;

    [ObservableProperty]
    private string? errorMessage;

    public HolidayExceptionWorkspaceViewModel(
        IHolidayExceptionWorkspaceQueryService queryService,
        IHolidayCalendarManagementService holidayManagementService,
        IWorkScheduleDateOverrideManagementService overrideManagementService,
        TimeProvider timeProvider)
    {
        _queryService =
            queryService;

        _holidayManagementService =
            holidayManagementService;

        _overrideManagementService =
            overrideManagementService;

        _timeProvider =
            timeProvider;

        int currentYear =
            _timeProvider
                .GetLocalNow()
                .Year;

        SelectedYear =
            currentYear;

        YearOptions =
            Enumerable
                .Range(
                    currentYear - 2,
                    6)
                .ToArray();

        HolidayEditorDate =
            CreateDefaultEditorDate();

        OverrideEditorWorkDate =
            CreateDefaultEditorDate();

        LoadCommand =
            new AsyncRelayCommand(
                LoadAsync);

        RefreshCommand =
            new AsyncRelayCommand(
                LoadAsync);

        NewHolidayCommand =
            new RelayCommand(
                BeginNewHoliday);

        SaveHolidayCommand =
            new AsyncRelayCommand(
                SaveHolidayAsync);

        DeactivateHolidayCommand =
            new AsyncRelayCommand(
                DeactivateHolidayAsync);

        ReactivateHolidayCommand =
            new AsyncRelayCommand(
                ReactivateHolidayAsync);

        NewOverrideCommand =
            new RelayCommand(
                BeginNewOverride);

        SaveOverrideCommand =
            new AsyncRelayCommand(
                SaveOverrideAsync);

        DeleteOverrideCommand =
            new AsyncRelayCommand(
                DeleteOverrideAsync);
    }

    public IAsyncRelayCommand LoadCommand
    {
        get;
    }

    public IAsyncRelayCommand RefreshCommand
    {
        get;
    }

    public IRelayCommand NewHolidayCommand
    {
        get;
    }

    public IAsyncRelayCommand SaveHolidayCommand
    {
        get;
    }

    public IAsyncRelayCommand DeactivateHolidayCommand
    {
        get;
    }

    public IAsyncRelayCommand ReactivateHolidayCommand
    {
        get;
    }

    public IRelayCommand NewOverrideCommand
    {
        get;
    }

    public IAsyncRelayCommand SaveOverrideCommand
    {
        get;
    }

    public IAsyncRelayCommand DeleteOverrideCommand
    {
        get;
    }

    public bool IsEditingExistingHoliday =>
        SelectedHolidayItem is not null;

    public bool IsEditingExistingOverride =>
        SelectedOverrideItem is not null;

    partial void OnSelectedHolidayItemChanged(
        HolidayExceptionWorkspaceHolidayItem? value)
    {
        if (value is null)
        {
            return;
        }

        HolidayEditorDate =
            value.Date.ToDateTime(
                TimeOnly.MinValue);

        HolidayEditorName =
            value.Name;
    }

    partial void OnSelectedOverrideItemChanged(
        HolidayExceptionWorkspaceOverrideItem? value)
    {
        if (value is null)
        {
            return;
        }

        OverrideEditorWorkDate =
            value.WorkDate.ToDateTime(
                TimeOnly.MinValue);

        OverrideEditorIsWorkingDay =
            value.IsWorkingDay;

        OverrideEditorStartTimeText =
            FormatTime(
                value.StartTime);

        OverrideEditorEndTimeText =
            FormatTime(
                value.EndTime);

        OverrideEditorBreakMinutesText =
            value.BreakMinutes
                .ToString(
                    CultureInfo.InvariantCulture);

        OverrideEditorNote =
            value.Note;
    }

    private async Task LoadAsync()
    {
        if (SelectedYear is < 1 or > 9999)
        {
            ErrorMessage =
                "Năm lịch không hợp lệ.";

            return;
        }

        IsLoading =
            true;

        ErrorMessage =
            null;

        try
        {
            Guid? selectedScheduleId =
                SelectedScheduleItem?.Id;

            HolidayExceptionWorkspaceSnapshot snapshot =
                await _queryService
                    .GetAsync(
                        new HolidayExceptionWorkspaceQuery(
                            SelectedYear,
                            selectedScheduleId));

            if (!selectedScheduleId.HasValue)
            {
                HolidayExceptionWorkspaceScheduleItem?
                    preferredSchedule =
                        snapshot.Schedules
                            .FirstOrDefault(
                                item =>
                                    item.IsActive)
                        ?? snapshot.Schedules
                            .FirstOrDefault();

                if (preferredSchedule is not null)
                {
                    selectedScheduleId =
                        preferredSchedule.Id;

                    snapshot =
                        await _queryService
                            .GetAsync(
                                new HolidayExceptionWorkspaceQuery(
                                    SelectedYear,
                                    selectedScheduleId));
                }
            }

            ApplySnapshot(
                snapshot);
        }
        catch (Exception exception)
        {
            ErrorMessage =
                exception.Message;
        }
        finally
        {
            IsLoading =
                false;
        }
    }

    private void ApplySnapshot(
        HolidayExceptionWorkspaceSnapshot snapshot)
    {
        HolidayItems =
            snapshot.Holidays;

        ScheduleItems =
            snapshot.Schedules;

        OverrideItems =
            snapshot.Overrides;

        SelectedScheduleItem =
            snapshot.SelectedWorkScheduleId.HasValue
                ? ScheduleItems
                    .FirstOrDefault(
                        item =>
                            item.Id ==
                            snapshot.SelectedWorkScheduleId.Value)
                : null;

        if (SelectedHolidayItem is not null)
        {
            SelectedHolidayItem =
                HolidayItems
                    .FirstOrDefault(
                        item =>
                            item.Id ==
                            SelectedHolidayItem.Id);
        }

        if (SelectedOverrideItem is not null)
        {
            SelectedOverrideItem =
                OverrideItems
                    .FirstOrDefault(
                        item =>
                            item.Id ==
                            SelectedOverrideItem.Id);
        }
    }

    private void BeginNewHoliday()
    {
        SelectedHolidayItem =
            null;

        HolidayEditorDate =
            CreateDefaultEditorDate();

        HolidayEditorName =
            null;

        ClearMessages();
    }

    private async Task SaveHolidayAsync()
    {
        ClearMessages();

        if (!HolidayEditorDate.HasValue)
        {
            ErrorMessage =
                "Vui lòng chọn ngày lễ.";

            return;
        }

        DateOnly holidayDate =
            DateOnly.FromDateTime(
                HolidayEditorDate.Value);

        if (holidayDate.Year !=
            SelectedYear)
        {
            ErrorMessage =
                "Ngày lễ phải thuộc năm đang xem.";

            return;
        }

        HolidayCalendarManagementResult result;

        if (SelectedHolidayItem is null)
        {
            result =
                await _holidayManagementService
                    .CreateAsync(
                        new CreateHolidayCalendarDayRequest(
                            holidayDate,
                            HolidayEditorName ?? string.Empty));
        }
        else
        {
            if (holidayDate !=
                SelectedHolidayItem.Date)
            {
                ErrorMessage =
                    "Không thể thay đổi ngày của ngày lễ đã có.";

                return;
            }

            result =
                await _holidayManagementService
                    .RenameAsync(
                        new RenameHolidayCalendarDayRequest(
                            SelectedHolidayItem.Id,
                            HolidayEditorName ?? string.Empty));
        }

        if (!result.IsSuccessful)
        {
            ErrorMessage =
                result.ErrorMessage
                ?? "Không thể lưu ngày lễ.";

            return;
        }

        OperationMessage =
            SelectedHolidayItem is null
                ? "Đã thêm ngày lễ."
                : "Đã cập nhật ngày lễ.";

        await LoadAsync();
    }

    private async Task DeactivateHolidayAsync()
    {
        ClearMessages();

        if (SelectedHolidayItem is null)
        {
            ErrorMessage =
                "Vui lòng chọn ngày lễ.";

            return;
        }

        HolidayCalendarManagementResult result =
            await _holidayManagementService
                .DeactivateAsync(
                    SelectedHolidayItem.Id);

        if (!result.IsSuccessful)
        {
            ErrorMessage =
                result.ErrorMessage
                ?? "Không thể ngừng áp dụng ngày lễ.";

            return;
        }

        OperationMessage =
            "Đã ngừng áp dụng ngày lễ.";

        await LoadAsync();
    }

    private async Task ReactivateHolidayAsync()
    {
        ClearMessages();

        if (SelectedHolidayItem is null)
        {
            ErrorMessage =
                "Vui lòng chọn ngày lễ.";

            return;
        }

        HolidayCalendarManagementResult result =
            await _holidayManagementService
                .ReactivateAsync(
                    SelectedHolidayItem.Id);

        if (!result.IsSuccessful)
        {
            ErrorMessage =
                result.ErrorMessage
                ?? "Không thể áp dụng lại ngày lễ.";

            return;
        }

        OperationMessage =
            "Đã áp dụng lại ngày lễ.";

        await LoadAsync();
    }

    private void BeginNewOverride()
    {
        SelectedOverrideItem =
            null;

        OverrideEditorWorkDate =
            CreateDefaultEditorDate();

        OverrideEditorIsWorkingDay =
            true;

        OverrideEditorStartTimeText =
            "08:00";

        OverrideEditorEndTimeText =
            "17:00";

        OverrideEditorBreakMinutesText =
            "60";

        OverrideEditorNote =
            null;

        ClearMessages();
    }

    private async Task SaveOverrideAsync()
    {
        ClearMessages();

        if (SelectedScheduleItem is null)
        {
            ErrorMessage =
                "Vui lòng chọn lịch làm việc.";

            return;
        }

        if (!OverrideEditorWorkDate.HasValue)
        {
            ErrorMessage =
                "Vui lòng chọn ngày ngoại lệ.";

            return;
        }

        DateOnly workDate =
            DateOnly.FromDateTime(
                OverrideEditorWorkDate.Value);

        if (workDate.Year !=
            SelectedYear)
        {
            ErrorMessage =
                "Ngày ngoại lệ phải thuộc năm đang xem.";

            return;
        }

        if (SelectedOverrideItem is not null
            && workDate !=
                SelectedOverrideItem.WorkDate)
        {
            ErrorMessage =
                "Không thể thay đổi ngày của ngoại lệ đã có.";

            return;
        }

        if (!TryReadOverrideExpectation(
                out TimeOnly? startTime,
                out TimeOnly? endTime,
                out int breakMinutes))
        {
            return;
        }

        WorkScheduleDateOverrideManagementResult result;

        if (SelectedOverrideItem is null)
        {
            result =
                await _overrideManagementService
                    .CreateAsync(
                        new CreateWorkScheduleDateOverrideRequest(
                            SelectedScheduleItem.Id,
                            workDate,
                            OverrideEditorIsWorkingDay,
                            startTime,
                            endTime,
                            breakMinutes,
                            OverrideEditorNote));
        }
        else
        {
            result =
                await _overrideManagementService
                    .UpdateAsync(
                        new UpdateWorkScheduleDateOverrideRequest(
                            SelectedOverrideItem.Id,
                            OverrideEditorIsWorkingDay,
                            startTime,
                            endTime,
                            breakMinutes,
                            OverrideEditorNote));
        }

        if (!result.IsSuccessful)
        {
            ErrorMessage =
                result.ErrorMessage
                ?? "Không thể lưu ngoại lệ lịch làm việc.";

            return;
        }

        OperationMessage =
            SelectedOverrideItem is null
                ? "Đã thêm ngoại lệ lịch làm việc."
                : "Đã cập nhật ngoại lệ lịch làm việc.";

        await LoadAsync();
    }

    private async Task DeleteOverrideAsync()
    {
        ClearMessages();

        if (SelectedOverrideItem is null)
        {
            ErrorMessage =
                "Vui lòng chọn ngoại lệ lịch làm việc.";

            return;
        }

        WorkScheduleDateOverrideManagementResult result =
            await _overrideManagementService
                .DeleteAsync(
                    SelectedOverrideItem.Id);

        if (!result.IsSuccessful)
        {
            ErrorMessage =
                result.ErrorMessage
                ?? "Không thể xóa ngoại lệ lịch làm việc.";

            return;
        }

        SelectedOverrideItem =
            null;

        OperationMessage =
            "Đã xóa ngoại lệ lịch làm việc.";

        await LoadAsync();
    }

    private bool TryReadOverrideExpectation(
        out TimeOnly? startTime,
        out TimeOnly? endTime,
        out int breakMinutes)
    {
        startTime =
            null;

        endTime =
            null;

        breakMinutes =
            0;

        if (!OverrideEditorIsWorkingDay)
        {
            return true;
        }

        if (!TryParseTime(
                OverrideEditorStartTimeText,
                out TimeOnly parsedStartTime))
        {
            ErrorMessage =
                "Giờ bắt đầu phải theo định dạng HH:mm.";

            return false;
        }

        if (!TryParseTime(
                OverrideEditorEndTimeText,
                out TimeOnly parsedEndTime))
        {
            ErrorMessage =
                "Giờ kết thúc phải theo định dạng HH:mm.";

            return false;
        }

        if (!int.TryParse(
                OverrideEditorBreakMinutesText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int parsedBreakMinutes)
            || parsedBreakMinutes < 0)
        {
            ErrorMessage =
                "Số phút nghỉ phải là số nguyên không âm.";

            return false;
        }

        startTime =
            parsedStartTime;

        endTime =
            parsedEndTime;

        breakMinutes =
            parsedBreakMinutes;

        return true;
    }

    private DateTime CreateDefaultEditorDate()
    {
        DateTime localToday =
            _timeProvider
                .GetLocalNow()
                .Date;

        if (localToday.Year ==
            SelectedYear)
        {
            return localToday;
        }

        return new DateTime(
            SelectedYear,
            1,
            1);
    }

    private static bool TryParseTime(
        string? text,
        out TimeOnly value)
    {
        return TimeOnly.TryParseExact(
            text?.Trim(),
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out value);
    }

    private static string? FormatTime(
        TimeOnly? value)
    {
        return value?.ToString(
            "HH:mm",
            CultureInfo.InvariantCulture);
    }

    private void ClearMessages()
    {
        ErrorMessage =
            null;

        OperationMessage =
            null;
    }
}
