using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Attendance.Schedules;

public sealed class AuthorizedWorkScheduleDayManagementService
    : IWorkScheduleDayManagementService
{
    private readonly IWorkScheduleDayManagementService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedWorkScheduleDayManagementService(
        IWorkScheduleDayManagementService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<WorkScheduleDayManagementResult> UpdateAsync(
        UpdateWorkScheduleDayRequest request,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.WorkScheduleEdit,
                cancellationToken);

        return await _inner.UpdateAsync(
            request,
            cancellationToken);
    }
}
