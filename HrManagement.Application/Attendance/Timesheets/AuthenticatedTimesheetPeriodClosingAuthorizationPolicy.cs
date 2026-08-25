namespace HrManagement.Application.Attendance.Timesheets;

public sealed class AuthenticatedTimesheetPeriodClosingAuthorizationPolicy
    : ITimesheetPeriodClosingAuthorizationPolicy
{
    public Task<bool> CanCloseAsync(
        TimesheetPeriodClosingAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        return Task.FromResult(
            !string.IsNullOrWhiteSpace(
                request.Actor.UserId)
            && !string.IsNullOrWhiteSpace(
                request.Actor.Username));
    }
}
