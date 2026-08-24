namespace HrManagement.Application.Attendance.Corrections;

public sealed record AttendanceCorrectionWorkspaceSnapshot(
    Guid AttendanceRecordId,
    Guid EmployeeId,
    DateOnly WorkDate,
    string TimeZoneId,
    IReadOnlyList<AttendanceCorrectionWorkspaceEventItem>
        EffectiveEvents,
    IReadOnlyList<AttendanceCorrectionWorkspaceHistoryItem>
        Corrections);
