namespace HrManagement.Application.Attendance.Records;

public sealed record AttendancePunchContextResolutionResult(
    bool IsSuccessful,
    AttendancePunchContext? Context = null,
    string? ErrorMessage = null);
