using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Payroll.Compensation;

public sealed class PermissionEmployeeCompensationAuthorizationPolicy
    : IEmployeeCompensationAuthorizationPolicy
{
    private readonly IAuthorizationService
        _authorizationService;

    public PermissionEmployeeCompensationAuthorizationPolicy(
        IAuthorizationService authorizationService)
    {
        _authorizationService =
            authorizationService;
    }

    public Task<bool> CanSetAsync(
        EmployeeCompensationAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        return _authorizationService
            .HasPermissionAsync(
                PermissionCodes.PayrollManageCompensation,
                cancellationToken);
    }
}
