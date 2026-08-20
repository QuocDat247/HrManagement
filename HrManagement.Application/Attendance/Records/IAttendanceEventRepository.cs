using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Application.Attendance.Records;

public interface IAttendanceEventRepository
{
    Task<IReadOnlyList<AttendanceEvent>>
        GetByAttendanceRecordIdAsync(
            Guid attendanceRecordId,
            CancellationToken cancellationToken = default);

    Task<AttendanceEvent?> GetLatestByEmployeeIdAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);
}
