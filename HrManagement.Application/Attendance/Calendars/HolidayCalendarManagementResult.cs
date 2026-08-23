namespace HrManagement.Application.Attendance.Calendars;

public sealed record HolidayCalendarManagementResult(
    bool IsSuccessful,
    Guid? HolidayCalendarDayId = null,
    string? ErrorMessage = null);
