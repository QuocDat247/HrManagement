using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Authentication.Accounts;

public sealed class AuthorizedStandardAccountCreationService
    : IStandardAccountCreationService
{
    private readonly IStandardAccountCreationService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedStandardAccountCreationService(
        IStandardAccountCreationService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<CreateStandardAccountResult> CreateAsync(
        CreateStandardAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.AccountCreate,
                cancellationToken);

        return await _inner
            .CreateAsync(
                request,
                cancellationToken);
    }
}
