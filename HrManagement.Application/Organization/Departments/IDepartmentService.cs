using HrManagement.Domain.Organization.Departments;

namespace HrManagement.Application.Organization.Departments;

public interface IDepartmentService
{
    Task<IReadOnlyList<Department>> GetDepartmentsAsync(
        CancellationToken cancellationToken = default);

    Task<DepartmentOperationResult> CreateDepartmentAsync(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken = default);

    Task<DepartmentOperationResult> UpdateDepartmentAsync(
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default);

    Task<DepartmentOperationResult> DeactivateDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<DepartmentOperationResult> ReactivateDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);
}
