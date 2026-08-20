using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Application.Attendance.Records;

public interface IAttendancePunchPersistence
{
    Task AppendAsync(
        AttendanceRecord? newRecord,
        AttendanceEvent newEvent,
        AttendanceEvent? expectedLastEvent,
        CancellationToken cancellationToken = default);
}
