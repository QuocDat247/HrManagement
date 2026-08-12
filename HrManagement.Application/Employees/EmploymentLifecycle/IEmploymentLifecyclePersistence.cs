using HrManagement.Domain.Employees;

namespace HrManagement.Application.Employees.EmploymentLifecycle;

public interface IEmploymentLifecyclePersistence
{
    Task CreateEmployeeWithPeriodAsync(
        Employee employee,
        EmploymentPeriod period,
        CancellationToken cancellationToken = default);

    Task UpdateEmployeeWithPeriodAsync(
        Employee employee,
        EmploymentPeriod period,
        CancellationToken cancellationToken = default);

    Task UpdateEmployeeWithNewPeriodAsync(
        Employee employee,
        EmploymentPeriod newPeriod,
        CancellationToken cancellationToken = default);
}
