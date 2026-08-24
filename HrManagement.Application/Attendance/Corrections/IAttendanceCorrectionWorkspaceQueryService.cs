namespace HrManagement.Application.Attendance.Corrections;

public interface IAttendanceCorrectionWorkspaceQueryService
{
    Task<AttendanceCorrectionWorkspaceSnapshot?> GetAsync(
        Guid attendanceRecordId,
        CancellationToken cancellationToken = default);
}
