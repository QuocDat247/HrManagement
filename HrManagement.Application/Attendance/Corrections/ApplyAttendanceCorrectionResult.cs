namespace HrManagement.Application.Attendance.Corrections;

public sealed record ApplyAttendanceCorrectionResult(
    bool IsSuccessful,
    Guid? AttendanceCorrectionId = null,
    Guid? AttendanceRecordId = null,
    Guid? AffectedEventId = null,
    int? Revision = null,
    string? ErrorMessage = null);
