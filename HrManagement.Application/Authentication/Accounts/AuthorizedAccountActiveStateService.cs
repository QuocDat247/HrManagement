using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Authentication.Accounts;

public sealed class AuthorizedAccountActiveStateService
    : IAccountActiveStateService
{
    private readonly IAccountActiveStateService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedAccountActiveStateService(
        IAccountActiveStateService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<SetAccountActiveStateResult> SetAsync(
        SetAccountActiveStateRequest request,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.AccountLock,
                cancellationToken);

        return await _inner
            .SetAsync(
                request,
                cancellationToken);
    }
}
