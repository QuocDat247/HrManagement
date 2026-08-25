namespace HrManagement.Application.Attendance.Timesheets;

public interface ITimesheetPeriodClosingAuthorizationPolicy
{
    Task<bool> CanCloseAsync(
        TimesheetPeriodClosingAuthorizationRequest request,
        CancellationToken cancellationToken = default);
}
