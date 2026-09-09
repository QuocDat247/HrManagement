using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Payroll.Periods;

public sealed class PermissionPayrollPeriodClosingAuthorizationPolicy
    : IPayrollPeriodClosingAuthorizationPolicy
{
    private readonly IAuthorizationService
        _authorizationService;

    public PermissionPayrollPeriodClosingAuthorizationPolicy(
        IAuthorizationService authorizationService)
    {
        _authorizationService =
            authorizationService;
    }

    public Task<bool> CanCloseAsync(
        PayrollPeriodClosingAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        return _authorizationService
            .HasPermissionAsync(
                PermissionCodes.PayrollClose,
                cancellationToken);
    }
}
