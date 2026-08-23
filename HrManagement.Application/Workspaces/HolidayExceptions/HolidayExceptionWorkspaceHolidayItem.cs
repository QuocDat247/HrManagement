namespace HrManagement.Application.Workspaces.HolidayExceptions;

public sealed record HolidayExceptionWorkspaceHolidayItem(
    Guid Id,
    DateOnly Date,
    string Name,
    bool IsActive);
