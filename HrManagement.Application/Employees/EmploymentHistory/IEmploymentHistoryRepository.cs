using HrManagement.Domain.Employees;

namespace HrManagement.Application.Employees.EmploymentHistories;

public interface IEmploymentHistoryRepository
{
    Task<EmploymentHistory> GetByEmployeeIdAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task AddPeriodAsync(
        EmploymentPeriod period,
        CancellationToken cancellationToken = default);

    Task UpdatePeriodAsync(
        EmploymentPeriod period,
        CancellationToken cancellationToken = default);
}
