using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Attendance.Calendars;

public sealed class AuthorizedHolidayCalendarManagementService
    : IHolidayCalendarManagementService
{
    private readonly IHolidayCalendarManagementService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedHolidayCalendarManagementService(
        IHolidayCalendarManagementService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<HolidayCalendarManagementResult> CreateAsync(
        CreateHolidayCalendarDayRequest request,
        CancellationToken cancellationToken = default)
    {
        await RequireAsync(
            PermissionCodes.HolidayExceptionCreate,
            cancellationToken);

        return await _inner.CreateAsync(
            request,
            cancellationToken);
    }

    public async Task<HolidayCalendarManagementResult> RenameAsync(
        RenameHolidayCalendarDayRequest request,
        CancellationToken cancellationToken = default)
    {
        await RequireAsync(
            PermissionCodes.HolidayExceptionEdit,
            cancellationToken);

        return await _inner.RenameAsync(
            request,
            cancellationToken);
    }

    public async Task<HolidayCalendarManagementResult> DeactivateAsync(
        Guid holidayCalendarDayId,
        CancellationToken cancellationToken = default)
    {
        await RequireAsync(
            PermissionCodes.HolidayExceptionManageLifecycle,
            cancellationToken);

        return await _inner.DeactivateAsync(
            holidayCalendarDayId,
            cancellationToken);
    }

    public async Task<HolidayCalendarManagementResult> ReactivateAsync(
        Guid holidayCalendarDayId,
        CancellationToken cancellationToken = default)
    {
        await RequireAsync(
            PermissionCodes.HolidayExceptionManageLifecycle,
            cancellationToken);

        return await _inner.ReactivateAsync(
            holidayCalendarDayId,
            cancellationToken);
    }

    private Task RequireAsync(
        string permissionCode,
        CancellationToken cancellationToken)
    {
        return _authorizationGuard
            .RequirePermissionAsync(
                permissionCode,
                cancellationToken);
    }
}
