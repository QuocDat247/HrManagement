using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Organization.Departments;

namespace HrManagement.Application.Organization.Departments;

public sealed class AuthorizedDepartmentService
    : IDepartmentService
{
    private readonly IDepartmentService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedDepartmentService(
        IDepartmentService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<IReadOnlyList<Department>>
        GetDepartmentsAsync(
            CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.DepartmentView,
                cancellationToken);

        return await _inner
            .GetDepartmentsAsync(
                cancellationToken);
    }

    public async Task<DepartmentOperationResult>
        CreateDepartmentAsync(
            CreateDepartmentRequest request,
            CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.DepartmentCreate,
                cancellationToken);

        return await _inner
            .CreateDepartmentAsync(
                request,
                cancellationToken);
    }

    public async Task<DepartmentOperationResult>
        UpdateDepartmentAsync(
            UpdateDepartmentRequest request,
            CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.DepartmentEdit,
                cancellationToken);

        return await _inner
            .UpdateDepartmentAsync(
                request,
                cancellationToken);
    }

    public async Task<DepartmentOperationResult>
        DeactivateDepartmentAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.DepartmentManageLifecycle,
                cancellationToken);

        return await _inner
            .DeactivateDepartmentAsync(
                departmentId,
                cancellationToken);
    }

    public async Task<DepartmentOperationResult>
        ReactivateDepartmentAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.DepartmentManageLifecycle,
                cancellationToken);

        return await _inner
            .ReactivateDepartmentAsync(
                departmentId,
                cancellationToken);
    }
}
