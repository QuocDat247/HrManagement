using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Authorization.Roles;

public sealed class AuthorizedRoleActiveStateService
    : IRoleActiveStateService
{
    private readonly IRoleActiveStateService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedRoleActiveStateService(
        IRoleActiveStateService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<RoleManagementResult> SetAsync(
        Guid roleId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.RoleManageLifecycle,
                cancellationToken);

        return await _inner
            .SetAsync(
                roleId,
                isActive,
                cancellationToken);
    }
}
