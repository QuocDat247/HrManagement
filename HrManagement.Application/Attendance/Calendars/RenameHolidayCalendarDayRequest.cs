namespace HrManagement.Application.Attendance.Calendars;

public sealed record RenameHolidayCalendarDayRequest(
    Guid HolidayCalendarDayId,
    string Name);
