using HrManagement.Domain.Employees;

namespace HrManagement.Application.Employees;

public sealed class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;

    public EmployeeService(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<IReadOnlyList<Employee>> GetEmployeesAsync(
        EmployeeFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Employee> employees =
            await _employeeRepository.GetAllAsync(cancellationToken);

        if (filter is null)
        {
            return employees;
        }

        IEnumerable<Employee> query = employees;

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            string searchText = filter.SearchText.Trim();

            query = query.Where(employee =>
                employee.EmployeeCode.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase)
                ||
                employee.FullName.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase)
                ||
                employee.Department.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase)
                ||
                employee.Position.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(
                employee => employee.Status == filter.Status.Value);
        }

        return query.ToList();
    }
}
