using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Attendance.Records;

public sealed class AuthorizedAttendancePunchService
    : IAttendancePunchService
{
    private readonly IAttendancePunchService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedAttendancePunchService(
        IAttendancePunchService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<RecordAttendancePunchResult> RecordAsync(
        RecordAttendancePunchRequest request,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.AttendanceEdit,
                cancellationToken);

        return await _inner
            .RecordAsync(
                request,
                cancellationToken);
    }
}
