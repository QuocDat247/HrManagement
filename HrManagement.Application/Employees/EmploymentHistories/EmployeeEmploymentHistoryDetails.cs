namespace HrManagement.Application.Employees.EmploymentHistories;

public sealed record EmployeeEmploymentHistoryDetails(
    Guid EmployeeId,
    IReadOnlyList<EmploymentHistoryPeriodItem> Periods);
