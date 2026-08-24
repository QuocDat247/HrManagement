using HrManagement.Domain.Attendance.Corrections;

namespace HrManagement.Application.Attendance.Corrections;

public interface IAttendanceCorrectionPersistence
{
    Task<IReadOnlyList<AttendanceCorrection>>
        GetByAttendanceRecordIdAsync(
            Guid attendanceRecordId,
            CancellationToken cancellationToken = default);

    Task AppendAsync(
        AttendanceCorrection correction,
        CancellationToken cancellationToken = default);
}
