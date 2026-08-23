using HrManagement.Domain.Attendance.Calendars;

namespace HrManagement.Application.Attendance.Calendars;

public interface IHolidayCalendarManagementPersistence
{
    Task<HolidayCalendarDay?> GetByIdAsync(
        Guid holidayCalendarDayId,
        CancellationToken cancellationToken = default);

    Task<HolidayCalendarDay?> GetByDateAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task CreateAsync(
        HolidayCalendarDay holiday,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        HolidayCalendarDay holiday,
        CancellationToken cancellationToken = default);
}
