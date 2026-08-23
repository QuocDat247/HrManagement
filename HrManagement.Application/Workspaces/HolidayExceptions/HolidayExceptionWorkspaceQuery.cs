namespace HrManagement.Application.Workspaces.HolidayExceptions;

public sealed record HolidayExceptionWorkspaceQuery(
    int Year,
    Guid? WorkScheduleId = null);
