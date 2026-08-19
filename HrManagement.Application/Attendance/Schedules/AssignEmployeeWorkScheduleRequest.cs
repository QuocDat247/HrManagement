namespace HrManagement.Application.Attendance.Schedules;

public sealed record AssignEmployeeWorkScheduleRequest(
    Guid EmployeeId,
    Guid WorkScheduleId,
    DateOnly EffectiveFrom);
