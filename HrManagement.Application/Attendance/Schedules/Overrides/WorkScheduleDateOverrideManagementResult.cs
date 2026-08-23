namespace HrManagement.Application.Attendance.Schedules.Overrides;

public sealed record WorkScheduleDateOverrideManagementResult(
    bool IsSuccessful,
    Guid? WorkScheduleDateOverrideId = null,
    string? ErrorMessage = null);
