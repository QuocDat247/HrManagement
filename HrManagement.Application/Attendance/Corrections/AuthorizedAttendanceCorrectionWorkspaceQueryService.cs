using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Attendance.Corrections;

public sealed class AuthorizedAttendanceCorrectionWorkspaceQueryService
    : IAttendanceCorrectionWorkspaceQueryService
{
    private readonly IAttendanceCorrectionWorkspaceQueryService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedAttendanceCorrectionWorkspaceQueryService(
        IAttendanceCorrectionWorkspaceQueryService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<AttendanceCorrectionWorkspaceSnapshot?> GetAsync(
        Guid attendanceRecordId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.AttendanceView,
                cancellationToken);

        return await _inner
            .GetAsync(
                attendanceRecordId,
                cancellationToken);
    }
}
