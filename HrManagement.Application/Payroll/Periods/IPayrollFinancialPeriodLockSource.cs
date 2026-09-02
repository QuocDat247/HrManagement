namespace HrManagement.Application.Payroll.Periods;

public interface IPayrollFinancialPeriodLockSource
{
    Task<bool> IsLockedAsync(
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null,
        CancellationToken cancellationToken = default);
}
