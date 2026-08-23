namespace HrManagement.Application.Attendance.Calendars;

public interface IHolidayCalendarManagementService
{
    Task<HolidayCalendarManagementResult> CreateAsync(
        CreateHolidayCalendarDayRequest request,
        CancellationToken cancellationToken = default);

    Task<HolidayCalendarManagementResult> RenameAsync(
        RenameHolidayCalendarDayRequest request,
        CancellationToken cancellationToken = default);

    Task<HolidayCalendarManagementResult> DeactivateAsync(
        Guid holidayCalendarDayId,
        CancellationToken cancellationToken = default);

    Task<HolidayCalendarManagementResult> ReactivateAsync(
        Guid holidayCalendarDayId,
        CancellationToken cancellationToken = default);
}
