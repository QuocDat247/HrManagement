using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Workspaces.HolidayExceptions;

public sealed class AuthorizedHolidayExceptionWorkspaceQueryService
    : IHolidayExceptionWorkspaceQueryService
{
    private readonly IHolidayExceptionWorkspaceQueryService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedHolidayExceptionWorkspaceQueryService(
        IHolidayExceptionWorkspaceQueryService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<HolidayExceptionWorkspaceSnapshot> GetAsync(
        HolidayExceptionWorkspaceQuery query,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.HolidayExceptionView,
                cancellationToken);

        return await _inner
            .GetAsync(
                query,
                cancellationToken);
    }
}
