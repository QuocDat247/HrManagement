using HrManagement.Domain.Employees;

namespace HrManagement.Application.Employees;

public interface IEmployeeRepository
{
    Task<IReadOnlyList<Employee>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Employee?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Employee?> GetByEmployeeCodeAsync(
        string employeeCode,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Employee employee,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Employee employee,
        CancellationToken cancellationToken = default);
}
