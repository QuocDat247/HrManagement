using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Authorization.Roles;

public sealed class AuthorizedRoleManagementService
    : IRoleManagementService
{
    private readonly IRoleManagementService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedRoleManagementService(
        IRoleManagementService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<RoleManagementResult> CreateAsync(
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.RoleCreate,
                cancellationToken);

        return await _inner
            .CreateAsync(
                name,
                description,
                cancellationToken);
    }

    public async Task<RoleManagementResult> UpdateAsync(
        Guid roleId,
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.RoleEdit,
                cancellationToken);

        return await _inner
            .UpdateAsync(
                roleId,
                name,
                description,
                cancellationToken);
    }
}
