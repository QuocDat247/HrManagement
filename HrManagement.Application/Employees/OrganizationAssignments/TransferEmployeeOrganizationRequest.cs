namespace HrManagement.Application.Employees.OrganizationAssignments;

public sealed record TransferEmployeeOrganizationRequest(
    Guid EmployeeId,
    Guid DepartmentId,
    Guid PositionId,
    DateOnly EffectiveDate);
