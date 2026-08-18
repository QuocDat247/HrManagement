using HrManagement.Application.Employees.Profiles.Completion;
using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.ViewModels;

public sealed class EmployeeListItemViewModel
{
    public Employee Employee
    {
        get;
    }

    public EmployeeProfileCompletionResult Completion
    {
        get;
    }

    public Guid Id =>
        Employee.Id;

    public string EmployeeCode =>
        Employee.EmployeeCode;

    public string FullName =>
        Employee.FullName;

    public string Department =>
        Employee.Department;

    public string Position =>
        Employee.Position;

    public EmployeeStatus Status =>
        Employee.Status;

    public bool RequiresProfileCompletion =>
        Completion.RequiresCompletion;

    public string ProfileWarningText =>
        EmployeeProfileCompletionPresentation
            .BuildWarningText(
                Completion);

    public EmployeeListItemViewModel(
        Employee employee,
        EmployeeProfileCompletionResult completion)
    {
        ArgumentNullException.ThrowIfNull(
            employee);

        ArgumentNullException.ThrowIfNull(
            completion);

        Employee =
            employee;

        Completion =
            completion;
    }
}
