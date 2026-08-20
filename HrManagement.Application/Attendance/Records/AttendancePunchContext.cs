namespace HrManagement.Application.Attendance.Records;

public sealed record AttendancePunchContext(
    Guid EmployeeId,
    Guid EmploymentPeriodId,
    Guid WorkScheduleAssignmentId,
    Guid WorkScheduleId,
    DateOnly WorkDate,
    string TimeZoneId,
    bool IsWorkingDay,
    TimeOnly? ExpectedStartTime,
    TimeOnly? ExpectedEndTime,
    int ExpectedBreakMinutes);
