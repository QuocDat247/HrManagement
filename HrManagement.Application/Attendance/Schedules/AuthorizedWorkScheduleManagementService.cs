using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Attendance.Schedules;

public sealed class AuthorizedWorkScheduleManagementService
    : IWorkScheduleManagementService
{
    private readonly IWorkScheduleManagementService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedWorkScheduleManagementService(
        IWorkScheduleManagementService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<WorkScheduleManagementResult> CreateAsync(
        CreateWorkScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        await RequireAsync(
            PermissionCodes.WorkScheduleCreate,
            cancellationToken);

        return await _inner.CreateAsync(
            request,
            cancellationToken);
    }

    public async Task<WorkScheduleManagementResult> CloneAsync(
        CloneWorkScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        await RequireAsync(
            PermissionCodes.WorkScheduleCreate,
            cancellationToken);

        return await _inner.CloneAsync(
            request,
            cancellationToken);
    }

    public async Task<WorkScheduleManagementResult> UpdateAsync(
        UpdateWorkScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        await RequireAsync(
            PermissionCodes.WorkScheduleEdit,
            cancellationToken);

        return await _inner.UpdateAsync(
            request,
            cancellationToken);
    }

    public async Task<WorkScheduleManagementResult> DeactivateAsync(
        Guid workScheduleId,
        CancellationToken cancellationToken = default)
    {
        await RequireAsync(
            PermissionCodes.WorkScheduleManageLifecycle,
            cancellationToken);

        return await _inner.DeactivateAsync(
            workScheduleId,
            cancellationToken);
    }

    public async Task<WorkScheduleManagementResult> ReactivateAsync(
        Guid workScheduleId,
        CancellationToken cancellationToken = default)
    {
        await RequireAsync(
            PermissionCodes.WorkScheduleManageLifecycle,
            cancellationToken);

        return await _inner.ReactivateAsync(
            workScheduleId,
            cancellationToken);
    }

    public async Task<WorkScheduleManagementResult> DeleteAsync(
        Guid workScheduleId,
        CancellationToken cancellationToken = default)
    {
        await RequireAsync(
            PermissionCodes.WorkScheduleDelete,
            cancellationToken);

        return await _inner.DeleteAsync(
            workScheduleId,
            cancellationToken);
    }

    private Task RequireAsync(
        string permissionCode,
        CancellationToken cancellationToken)
    {
        return _authorizationGuard
            .RequirePermissionAsync(
                permissionCode,
                cancellationToken);
    }
}
