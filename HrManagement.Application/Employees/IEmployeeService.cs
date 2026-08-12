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
        DateOnly? terminationDate = null,
        CancellationToken cancellationToken = default);

    Task<CancelEmployeeDeactivationResult>
    CancelDeactivationAsync(
        Guid employeeId,
        EmployeeStatus restoredStatus,
        CancellationToken cancellationToken = default);

    Task<RehireEmployeeResult> RehireEmployeeAsync(
        Guid employeeId,
        DateOnly rehireDate,
        EmployeeStatus rehireStatus,
        CancellationToken cancellationToken = default);
}
