using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Attendance.Generation;

public sealed class AuthorizedDailyAttendanceGenerationService
    : IDailyAttendanceGenerationService
{
    private readonly IDailyAttendanceGenerationService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedDailyAttendanceGenerationService(
        IDailyAttendanceGenerationService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<GenerateDailyAttendanceResult> GenerateAsync(
        GenerateDailyAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.AttendanceEdit,
                cancellationToken);

        return await _inner
            .GenerateAsync(
                request,
                cancellationToken);
    }
}
