namespace HrManagement.Application.Attendance.Corrections;

public interface IAttendanceCorrectionService
{
    Task<ApplyAttendanceCorrectionResult> ApplyAsync(
        ApplyAttendanceCorrectionRequest request,
        CancellationToken cancellationToken = default);
}
