using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Workspaces.WorkSchedules;

public sealed class AuthorizedWorkScheduleWorkspaceQueryService
    : IWorkScheduleWorkspaceQueryService
{
    private readonly IWorkScheduleWorkspaceQueryService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedWorkScheduleWorkspaceQueryService(
        IWorkScheduleWorkspaceQueryService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<IReadOnlyList<WorkScheduleWorkspaceEmployeeItem>>
        GetEmployeesAsync(
            CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.WorkScheduleView,
                cancellationToken);

        return await _inner
            .GetEmployeesAsync(
                cancellationToken);
    }

    public async Task<WorkScheduleWorkspaceSnapshot>
        GetAsync(
            WorkScheduleWorkspaceQuery query,
            CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.WorkScheduleView,
                cancellationToken);

        return await _inner
            .GetAsync(
                query,
                cancellationToken);
    }
}
