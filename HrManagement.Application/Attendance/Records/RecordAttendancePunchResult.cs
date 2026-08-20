namespace HrManagement.Application.Attendance.Records;

public sealed record RecordAttendancePunchResult(
    bool IsSuccessful,
    Guid? AttendanceRecordId = null,
    Guid? AttendanceEventId = null,
    DateOnly? WorkDate = null,
    string? ErrorMessage = null);
