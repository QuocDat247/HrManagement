namespace HrManagement.Application.Authorization;

public sealed class AuthorizationGuard
    : IAuthorizationGuard
{
    private readonly IAuthorizationService
        _authorizationService;

    public AuthorizationGuard(
        IAuthorizationService authorizationService)
    {
        _authorizationService =
            authorizationService;
    }

    public async Task RequirePermissionAsync(
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        bool authorized =
            await _authorizationService
                .HasPermissionAsync(
                    permissionCode,
                    cancellationToken);

        if (!authorized)
        {
            throw new AuthorizationDeniedException(
                permissionCode);
        }
    }
}
