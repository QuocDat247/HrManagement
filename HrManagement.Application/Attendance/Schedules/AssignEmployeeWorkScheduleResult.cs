namespace HrManagement.Application.Attendance.Schedules;

public sealed record AssignEmployeeWorkScheduleResult(
    bool IsSuccessful,
    string? ErrorMessage = null);
