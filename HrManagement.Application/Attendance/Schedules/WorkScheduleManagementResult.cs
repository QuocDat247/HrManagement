namespace HrManagement.Application.Attendance.Schedules;

public sealed record WorkScheduleManagementResult(
    bool IsSuccessful,
    Guid? WorkScheduleId = null,
    string? ErrorMessage = null);
