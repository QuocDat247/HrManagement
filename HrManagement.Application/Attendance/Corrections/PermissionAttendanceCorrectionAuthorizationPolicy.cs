using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Attendance.Corrections;

public sealed class PermissionAttendanceCorrectionAuthorizationPolicy
    : IAttendanceCorrectionAuthorizationPolicy
{
    private readonly IAuthorizationService
        _authorizationService;

    public PermissionAttendanceCorrectionAuthorizationPolicy(
        IAuthorizationService authorizationService)
    {
        _authorizationService =
            authorizationService;
    }

    public Task<bool> CanApplyAsync(
        AttendanceCorrectionAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        return _authorizationService
            .HasPermissionAsync(
                PermissionCodes.AttendanceEdit,
                cancellationToken);
    }
}
