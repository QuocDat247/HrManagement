using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Attendance.Schedules;

public sealed class AuthorizedEmployeeWorkScheduleAssignmentService
    : IEmployeeWorkScheduleAssignmentService
{
    private readonly IEmployeeWorkScheduleAssignmentService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedEmployeeWorkScheduleAssignmentService(
        IEmployeeWorkScheduleAssignmentService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<AssignEmployeeWorkScheduleResult> AssignAsync(
        AssignEmployeeWorkScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.WorkScheduleAssign,
                cancellationToken);

        return await _inner.AssignAsync(
            request,
            cancellationToken);
    }
}
