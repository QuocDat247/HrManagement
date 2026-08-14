using HrManagement.Domain.Organization.Departments;

namespace HrManagement.Application.Organization.Departments;

public interface IDepartmentRepository
{
    Task<IReadOnlyList<Department>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Department?> GetByIdAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<Department?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Department department,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Department department,
        CancellationToken cancellationToken = default);
}
