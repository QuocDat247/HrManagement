using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Attendance.Timesheets;

public sealed class AuthorizedMonthlyTimesheetQueryService
    : IMonthlyTimesheetQueryService
{
    private readonly IMonthlyTimesheetQueryService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedMonthlyTimesheetQueryService(
        IMonthlyTimesheetQueryService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<MonthlyTimesheetReadModel> GetAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.TimesheetView,
                cancellationToken);

        return await _inner
            .GetAsync(
                year,
                month,
                cancellationToken);
    }
}
