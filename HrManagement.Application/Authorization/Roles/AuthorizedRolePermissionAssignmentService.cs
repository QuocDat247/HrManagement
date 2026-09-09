using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Authorization.Roles;

public sealed class AuthorizedRolePermissionAssignmentService
    : IRolePermissionAssignmentService
{
    private readonly IRolePermissionAssignmentService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedRolePermissionAssignmentService(
        IRolePermissionAssignmentService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<RoleManagementResult> ReplaceAsync(
        Guid roleId,
        IReadOnlyCollection<string> permissionCodes,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.RoleAssignPermission,
                cancellationToken);

        return await _inner
            .ReplaceAsync(
                roleId,
                permissionCodes,
                cancellationToken);
    }
}
