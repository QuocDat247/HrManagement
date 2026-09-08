using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Application.Organization.Positions;

public sealed class AuthorizedPositionService
    : IPositionService
{
    private readonly IPositionService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedPositionService(
        IPositionService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<IReadOnlyList<Position>>
        GetPositionsAsync(
            CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.PositionView,
                cancellationToken);

        return await _inner
            .GetPositionsAsync(
                cancellationToken);
    }

    public async Task<PositionOperationResult>
        CreatePositionAsync(
            CreatePositionRequest request,
            CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.PositionCreate,
                cancellationToken);

        return await _inner
            .CreatePositionAsync(
                request,
                cancellationToken);
    }

    public async Task<PositionOperationResult>
        UpdatePositionAsync(
            UpdatePositionRequest request,
            CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.PositionEdit,
                cancellationToken);

        return await _inner
            .UpdatePositionAsync(
                request,
                cancellationToken);
    }

    public async Task<PositionOperationResult>
        DeactivatePositionAsync(
            Guid positionId,
            CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.PositionManageLifecycle,
                cancellationToken);

        return await _inner
            .DeactivatePositionAsync(
                positionId,
                cancellationToken);
    }

    public async Task<PositionOperationResult>
        ReactivatePositionAsync(
            Guid positionId,
            CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.PositionManageLifecycle,
                cancellationToken);

        return await _inner
            .ReactivatePositionAsync(
                positionId,
                cancellationToken);
    }
}
