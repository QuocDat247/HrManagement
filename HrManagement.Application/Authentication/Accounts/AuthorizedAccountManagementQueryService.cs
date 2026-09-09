using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Authentication.Accounts;

public sealed class AuthorizedAccountManagementQueryService
    : IAccountManagementQueryService
{
    private readonly IAccountManagementQueryService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedAccountManagementQueryService(
        IAccountManagementQueryService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<AccountManagementSnapshot> GetAsync(
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.AccountView,
                cancellationToken);

        return await _inner
            .GetAsync(
                cancellationToken);
    }
}
