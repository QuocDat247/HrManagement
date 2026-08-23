namespace HrManagement.Application.Attendance.Calendars;

public sealed record CreateHolidayCalendarDayRequest(
    DateOnly Date,
    string Name);
