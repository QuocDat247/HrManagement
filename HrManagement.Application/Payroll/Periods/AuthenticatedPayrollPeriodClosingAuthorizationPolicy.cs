namespace HrManagement.Application.Payroll.Periods;

public sealed class AuthenticatedPayrollPeriodClosingAuthorizationPolicy
    : IPayrollPeriodClosingAuthorizationPolicy
{
    public Task<bool> CanCloseAsync(
        PayrollPeriodClosingAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        return Task.FromResult(
            !string.IsNullOrWhiteSpace(
                request.Actor.UserId)
            && !string.IsNullOrWhiteSpace(
                request.Actor.Username));
    }
}
