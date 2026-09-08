using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Employees;

namespace HrManagement.Application.Employees;

public sealed class AuthorizedEmployeeService
    : IEmployeeService
{
    private readonly IEmployeeService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedEmployeeService(
        IEmployeeService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<IReadOnlyList<Employee>>
        GetEmployeesAsync(
            EmployeeFilter? filter = null,
            CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.EmployeeView,
                cancellationToken);

        return await _inner
            .GetEmployeesAsync(
                filter,
                cancellationToken);
    }

    public async Task<CreateEmployeeResult>
        CreateEmployeeAsync(
            CreateEmployeeRequest request,
            CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.EmployeeCreate,
                cancellationToken);

        return await _inner
            .CreateEmployeeAsync(
                request,
                cancellationToken);
    }

    public async Task<UpdateEmployeeResult>
        UpdateEmployeeAsync(
            UpdateEmployeeRequest request,
            CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.EmployeeEdit,
                cancellationToken);

        return await _inner
            .UpdateEmployeeAsync(
                request,
                cancellationToken);
    }

    public async Task<DeactivateEmployeeResult>
        DeactivateEmployeeAsync(
            Guid employeeId,
            DateOnly? terminationDate = null,
            CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.EmployeeManageLifecycle,
                cancellationToken);

        return await _inner
            .DeactivateEmployeeAsync(
                employeeId,
                terminationDate,
                cancellationToken);
    }

    public async Task<CancelEmployeeDeactivationResult>
        CancelDeactivationAsync(
            Guid employeeId,
            EmployeeStatus restoredStatus,
            CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.EmployeeEdit,
                cancellationToken);

        return await _inner
            .CancelDeactivationAsync(
                employeeId,
                restoredStatus,
                cancellationToken);
    }

    public async Task<RehireEmployeeResult>
        RehireEmployeeAsync(
            Guid employeeId,
            DateOnly rehireDate,
            EmployeeStatus rehireStatus,
            CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.EmployeeEdit,
                cancellationToken);

        return await _inner
            .RehireEmployeeAsync(
                employeeId,
                rehireDate,
                rehireStatus,
                cancellationToken);
    }
}
