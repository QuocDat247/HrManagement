using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Workspaces.Overtime;

public sealed class AuthorizedOvertimeWorkspaceQueryService
    : IOvertimeWorkspaceQueryService
{
    private readonly IOvertimeWorkspaceQueryService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedOvertimeWorkspaceQueryService(
        IOvertimeWorkspaceQueryService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<OvertimeWorkspaceSnapshot> GetAsync(
        OvertimeWorkspaceQuery query,
        CancellationToken cancellationToken = default)
    {
        await RequireViewAsync(
            cancellationToken);

        return await _inner.GetAsync(
            query,
            cancellationToken);
    }

    public async Task<IReadOnlyList<OvertimeEmployeeOption>>
        GetEmployeesAsync(
            CancellationToken cancellationToken = default)
    {
        await RequireViewAsync(
            cancellationToken);

        return await _inner.GetEmployeesAsync(
            cancellationToken);
    }

    public async Task<IReadOnlyList<OvertimeStatusHistoryItem>>
        GetHistoryAsync(
            Guid overtimeRequestId,
            CancellationToken cancellationToken = default)
    {
        await RequireViewAsync(
            cancellationToken);

        return await _inner.GetHistoryAsync(
            overtimeRequestId,
            cancellationToken);
    }

    private Task RequireViewAsync(
        CancellationToken cancellationToken)
    {
        return _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.OvertimeView,
                cancellationToken);
    }
}
