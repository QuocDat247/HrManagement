using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Authorization.Roles;

public sealed class AuthorizedRolePermissionManagementQueryService
    : IRolePermissionManagementQueryService
{
    private readonly IRolePermissionManagementQueryService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedRolePermissionManagementQueryService(
        IRolePermissionManagementQueryService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<RolePermissionManagementSnapshot?> GetAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.RoleAssignPermission,
                cancellationToken);

        return await _inner
            .GetAsync(
                roleId,
                cancellationToken);
    }
}
