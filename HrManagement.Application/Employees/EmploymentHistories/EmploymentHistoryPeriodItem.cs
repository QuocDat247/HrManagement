namespace HrManagement.Application.Employees.EmploymentHistories;

public sealed record EmploymentHistoryPeriodItem(
    Guid Id,
    int SequenceNumber,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsOpen);
