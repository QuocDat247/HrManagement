namespace HrManagement.Application.Employees.OrganizationAssignments;

public sealed record EmployeeOrganizationAssignmentHistoryDetails(
    Guid EmployeeId,
    IReadOnlyList<OrganizationAssignmentHistoryItem> Assignments);
