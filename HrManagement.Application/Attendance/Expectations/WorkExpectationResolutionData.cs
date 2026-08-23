using HrManagement.Domain.Attendance.Calendars;
using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Application.Attendance.Expectations;

public sealed record WorkExpectationResolutionData(
    HolidayCalendarDay? Holiday,
    IReadOnlyList<WorkScheduleDay> WeeklyDays,
    IReadOnlyList<WorkScheduleDateOverride> DateOverrides);
