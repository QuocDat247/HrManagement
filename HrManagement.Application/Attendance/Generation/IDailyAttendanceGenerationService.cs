namespace HrManagement.Application.Attendance.Generation;

public interface IDailyAttendanceGenerationService
{
    Task<GenerateDailyAttendanceResult> GenerateAsync(
        GenerateDailyAttendanceRequest request,
        CancellationToken cancellationToken = default);
}
