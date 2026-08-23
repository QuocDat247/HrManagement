namespace HrManagement.Application.Attendance.Generation;

public sealed record DailyAttendanceGenerationCandidate(
    Guid EmployeeId,
    Guid EmploymentPeriodId,
    Guid WorkScheduleAssignmentId,
    Guid WorkScheduleId,
    string TimeZoneId);
