using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Application.Attendance.Records;

public interface IAttendanceRecordRepository
{
    Task<AttendanceRecord?> GetByIdAsync(
        Guid attendanceRecordId,
        CancellationToken cancellationToken = default);

    Task<AttendanceRecord?> GetByEmployeeAndWorkDateAsync(
        Guid employeeId,
        DateOnly workDate,
        CancellationToken cancellationToken = default);
}
