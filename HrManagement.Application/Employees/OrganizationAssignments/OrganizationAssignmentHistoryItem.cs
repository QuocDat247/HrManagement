namespace HrManagement.Application.Employees.OrganizationAssignments;

public sealed record OrganizationAssignmentHistoryItem(
    Guid Id,
    Guid EmploymentPeriodId,
    int SequenceNumber,
    Guid DepartmentId,
    string DepartmentCode,
    string DepartmentName,
    Guid PositionId,
    string PositionCode,
    string PositionName,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsOpen,
    bool IsBaseline);
