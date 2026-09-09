using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Payroll.Periods;

public sealed class AuthorizedClosedPayrollQueryService
    : IClosedPayrollQueryService
{
    private readonly IClosedPayrollQueryService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedClosedPayrollQueryService(
        IClosedPayrollQueryService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<ClosedPayrollReadModel?> GetAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.PayrollView,
                cancellationToken);

        return await _inner
            .GetAsync(
                year,
                month,
                cancellationToken);
    }
}
