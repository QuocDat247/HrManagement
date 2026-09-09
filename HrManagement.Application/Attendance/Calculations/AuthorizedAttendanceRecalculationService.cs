using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Attendance.Calculations;

public sealed class AuthorizedAttendanceRecalculationService
    : IAttendanceRecalculationService
{
    private readonly IAttendanceRecalculationService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedAttendanceRecalculationService(
        IAttendanceRecalculationService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<RecalculateAttendanceResult> RecalculateAsync(
        RecalculateAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.AttendanceEdit,
                cancellationToken);

        return await _inner
            .RecalculateAsync(
                request,
                cancellationToken);
    }
}
