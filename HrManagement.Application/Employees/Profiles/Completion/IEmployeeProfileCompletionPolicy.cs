namespace HrManagement.Application.Employees.Profiles.Completion;

public interface IEmployeeProfileCompletionPolicy
{
    EmployeeProfileCompletionResult Evaluate(
        EmployeeProfileCompletionData data);
}
