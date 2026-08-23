namespace HrManagement.Application.Workspaces.HolidayExceptions;

public sealed record HolidayExceptionWorkspaceSnapshot(
    int Year,
    Guid? SelectedWorkScheduleId,
    IReadOnlyList<HolidayExceptionWorkspaceHolidayItem> Holidays,
    IReadOnlyList<HolidayExceptionWorkspaceScheduleItem> Schedules,
    IReadOnlyList<HolidayExceptionWorkspaceOverrideItem> Overrides);
