using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Authentication.Accounts;

public sealed class AuthorizedAccountProfileUpdateService
    : IAccountProfileUpdateService
{
    private readonly IAccountProfileUpdateService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedAccountProfileUpdateService(
        IAccountProfileUpdateService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<UpdateAccountProfileResult> UpdateAsync(
        UpdateAccountProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.AccountEdit,
                cancellationToken);

        return await _inner
            .UpdateAsync(
                request,
                cancellationToken);
    }
}
