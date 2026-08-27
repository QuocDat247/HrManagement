using HrManagement.Domain.Employees;

namespace HrManagement.Application.Overtime.Requests;

public interface IOvertimeRequestSubmissionContextSource
{
    Task<EmploymentPeriod?> GetEmploymentPeriodAsync(
        Guid employeeId,
        DateOnly workDate,
        CancellationToken cancellationToken = default);
}
