namespace HrManagement.Application.Payroll.Calculations;

public sealed record BaseSalaryProrationComponent(
    Guid CompensationId,
    DateOnly AppliedFrom,
    DateOnly AppliedTo,
    int CoveredCalendarDays,
    int PeriodCalendarDays,
    decimal MonthlyBaseSalary,
    decimal ProratedAmount);
