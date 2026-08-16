namespace HrManagement.Application.Employees.OrganizationAssignments;

public sealed record TransferEmployeeOrganizationResult(
    bool IsSuccessful,
    string? ErrorMessage = null);
