using HrManagement.Domain.Employees;

namespace HrManagement.Application.Employees;

public interface IEmployeeService
{
    Task<IReadOnlyList<Employee>> GetEmployeesAsync(
        CancellationToken cancellationToken = default);
}
