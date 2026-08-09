using HrManagement.Domain.Employees;

namespace HrManagement.Application.Employees;

public sealed class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;

    public EmployeeService(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public Task<IReadOnlyList<Employee>> GetEmployeesAsync(
        CancellationToken cancellationToken = default)
    {
        return _employeeRepository.GetAllAsync(cancellationToken);
    }
}
