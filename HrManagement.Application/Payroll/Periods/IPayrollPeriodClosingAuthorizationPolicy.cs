namespace HrManagement.Application.Payroll.Periods;

public interface IPayrollPeriodClosingAuthorizationPolicy
{
    Task<bool> CanCloseAsync(
        PayrollPeriodClosingAuthorizationRequest request,
        CancellationToken cancellationToken = default);
}
