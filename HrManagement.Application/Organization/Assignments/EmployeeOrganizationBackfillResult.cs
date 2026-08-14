namespace HrManagement.Application.Organization.Assignments;

// Result contract
public sealed record EmployeeOrganizationBackfillResult(
    int ScannedEmployees,
    int UpdatedEmployees,
    int AssignedDepartmentReferences,
    int AssignedPositionReferences,
    int UnresolvedDepartmentReferences,
    int UnresolvedPositionReferences,
    int AmbiguousDepartmentReferences,
    int AmbiguousPositionReferences);
