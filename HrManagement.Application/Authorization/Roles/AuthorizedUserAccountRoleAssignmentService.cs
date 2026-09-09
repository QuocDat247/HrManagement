using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Authorization.Roles;

public sealed class AuthorizedUserAccountRoleAssignmentService
    : IUserAccountRoleAssignmentService
{
    private readonly IUserAccountRoleAssignmentService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedUserAccountRoleAssignmentService(
        IUserAccountRoleAssignmentService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<AccountRoleAssignmentResult> ReplaceAsync(
        ReplaceAccountRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.AccountAssignRole,
                cancellationToken);

        return await _inner
            .ReplaceAsync(
                request,
                cancellationToken);
    }
}
