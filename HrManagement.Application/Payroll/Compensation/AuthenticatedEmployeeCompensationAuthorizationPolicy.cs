namespace HrManagement.Application.Payroll.Compensation;

public sealed class AuthenticatedEmployeeCompensationAuthorizationPolicy
    : IEmployeeCompensationAuthorizationPolicy
{
    public Task<bool> CanSetAsync(
        EmployeeCompensationAuthorizationRequest request,
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
