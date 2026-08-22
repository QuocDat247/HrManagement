namespace HrManagement.Application.Attendance.Schedules;

public sealed record WorkScheduleDayManagementResult(
    bool IsSuccessful,
    string? ErrorMessage = null);
