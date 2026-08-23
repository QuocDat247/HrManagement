namespace HrManagement.Application.Workspaces.HolidayExceptions;

public sealed record HolidayExceptionWorkspaceScheduleItem(
    Guid Id,
    string Code,
    string Name,
    string TimeZoneId,
    bool IsActive);
