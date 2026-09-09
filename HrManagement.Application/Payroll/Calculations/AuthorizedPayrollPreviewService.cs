using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Payroll.Calculations;

public sealed class AuthorizedPayrollPreviewService
    : IPayrollPreviewService
{
    private readonly IPayrollPreviewService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedPayrollPreviewService(
        IPayrollPreviewService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<PayrollPreview> GetAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.PayrollCalculate,
                cancellationToken);

        return await _inner
            .GetAsync(
                year,
                month,
                cancellationToken);
    }
}
