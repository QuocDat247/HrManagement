namespace HrManagement.Application.Payroll.Compensation;

public sealed record SetEmployeeCompensationRequest(
    Guid EmployeeId,
    DateOnly EffectiveFrom,
    decimal MonthlyBaseSalary,
    string CurrencyCode);
