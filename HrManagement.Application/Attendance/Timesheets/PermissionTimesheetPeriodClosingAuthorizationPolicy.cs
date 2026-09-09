using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Attendance.Timesheets;

public sealed class PermissionTimesheetPeriodClosingAuthorizationPolicy
    : ITimesheetPeriodClosingAuthorizationPolicy
{
    private readonly IAuthorizationService
        _authorizationService;

    public PermissionTimesheetPeriodClosingAuthorizationPolicy(
        IAuthorizationService authorizationService)
    {
        _authorizationService =
            authorizationService;
    }

    public Task<bool> CanCloseAsync(
        TimesheetPeriodClosingAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        return _authorizationService
            .HasPermissionAsync(
                PermissionCodes.TimesheetClose,
                cancellationToken);
    }
}
