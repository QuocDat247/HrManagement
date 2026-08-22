using HrManagement.Application.Attendance.Generation;

namespace HrManagement.Tests.Attendance;

internal sealed class NoOpDailyAttendanceGenerationService
    : IDailyAttendanceGenerationService
{
    public Task<GenerateDailyAttendanceResult> GenerateAsync(
        GenerateDailyAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            new GenerateDailyAttendanceResult(
                true));
    }
}
