using HrManagement.Domain.Employees;

namespace HrManagement.Application.Employees;

public interface IEmployeeService
{
    Task<IReadOnlyList<Employee>> GetEmployeesAsync(
        EmployeeFilter? filter = null,
        CancellationToken cancellationToken = default);

    Task<CreateEmployeeResult> CreateEmployeeAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default);

    Task<UpdateEmployeeResult> UpdateEmployeeAsync(
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default);

    Task<DeactivateEmployeeResult> DeactivateEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);
}
