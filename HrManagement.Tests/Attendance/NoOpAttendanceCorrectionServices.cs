using HrManagement.Application.Attendance.Corrections;

namespace HrManagement.Tests.Attendance;

internal sealed class NoOpAttendanceCorrectionService
    : IAttendanceCorrectionService
{
    public Task<ApplyAttendanceCorrectionResult> ApplyAsync(
        ApplyAttendanceCorrectionRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            new ApplyAttendanceCorrectionResult(
                IsSuccessful: false,
                ErrorMessage:
                    "Correction service is not used by this test."));
    }
}

internal sealed class NoOpAttendanceCorrectionWorkspaceQueryService
    : IAttendanceCorrectionWorkspaceQueryService
{
    public Task<AttendanceCorrectionWorkspaceSnapshot?> GetAsync(
        Guid attendanceRecordId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<
            AttendanceCorrectionWorkspaceSnapshot?>(
                null);
    }
}
