using HrManagement.Domain.Employees;

namespace HrManagement.Application.Employees.Profiles.Completion;

public interface IEmployeeProfileCompletionService
{
    Task<EmployeeProfileCompletionResult>
        EvaluateAsync(
            Employee employee,
            CancellationToken cancellationToken = default);
}
