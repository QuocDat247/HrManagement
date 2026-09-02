namespace HrManagement.Application.Payroll.Compensation;

public sealed record EmployeeCompensationSegment(
    Guid CompensationId,
    Guid EmployeeId,
    Guid EmploymentPeriodId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    decimal MonthlyBaseSalary,
    string CurrencyCode);
