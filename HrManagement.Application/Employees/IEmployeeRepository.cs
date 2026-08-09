using HrManagement.Domain.Employees;

namespace HrManagement.Application.Employees;

public interface IEmployeeRepository
{
    Task<IReadOnlyList<Employee>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
