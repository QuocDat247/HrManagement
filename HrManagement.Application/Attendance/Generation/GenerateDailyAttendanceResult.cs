namespace HrManagement.Application.Attendance.Generation;

public sealed record GenerateDailyAttendanceResult(
    bool IsSuccessful,
    int CreatedCount = 0,
    int SkippedExistingCount = 0,
    string? ErrorMessage = null);
