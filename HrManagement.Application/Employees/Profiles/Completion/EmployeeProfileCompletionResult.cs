namespace HrManagement.Application.Employees.Profiles.Completion;

public sealed record EmployeeProfileCompletionResult(
    bool IsComplete,
    bool RequiresCompletion,
    IReadOnlyList<EmployeeProfileRequirement> MissingRequirements);
