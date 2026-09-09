using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Workspaces.AttendanceLeave;

public sealed class AuthorizedAttendanceLeaveWorkspaceQueryService
    : IAttendanceLeaveWorkspaceQueryService
{
    private readonly IAttendanceLeaveWorkspaceQueryService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedAttendanceLeaveWorkspaceQueryService(
        IAttendanceLeaveWorkspaceQueryService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<IReadOnlyList<AttendanceLeaveEmployeeItem>>
        GetEmployeesAsync(
            CancellationToken cancellationToken = default)
    {
        await RequireWorkspaceViewAsync(
            cancellationToken);

        return await _inner
            .GetEmployeesAsync(
                cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveTypeWorkspaceOption>>
        GetActiveLeaveTypesAsync(
            CancellationToken cancellationToken = default)
    {
        await RequireWorkspaceViewAsync(
            cancellationToken);

        return await _inner
            .GetActiveLeaveTypesAsync(
                cancellationToken);
    }

    public async Task<AttendanceLeaveWorkspaceSnapshot> GetAsync(
        AttendanceLeaveWorkspaceQuery query,
        CancellationToken cancellationToken = default)
    {
        await RequireWorkspaceViewAsync(
            cancellationToken);

        return await _inner
            .GetAsync(
                query,
                cancellationToken);
    }

    private async Task RequireWorkspaceViewAsync(
        CancellationToken cancellationToken)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.AttendanceView,
                cancellationToken);

        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.LeaveView,
                cancellationToken);
    }
}
