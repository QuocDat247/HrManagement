using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Application.Attendance.Calculations;

public interface IAttendanceCalculationPersistence
{
    Task ApplyAsync(
        AttendanceRecord calculatedRecord,
        IReadOnlyList<AttendanceEvent> expectedEvents,
        CancellationToken cancellationToken = default);
}
