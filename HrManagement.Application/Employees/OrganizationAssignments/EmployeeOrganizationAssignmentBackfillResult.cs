namespace HrManagement.Application.Employees.OrganizationAssignments;

public sealed record EmployeeOrganizationAssignmentBackfillResult(
    int ScannedEmployees,
    int CreatedAssignments,
    int SkippedExistingHistory,
    int SkippedMissingOrganizationReferences,
    int SkippedMissingMasterData,
    int SkippedMissingEmploymentPeriod,
    int SkippedInconsistentEmploymentState);
