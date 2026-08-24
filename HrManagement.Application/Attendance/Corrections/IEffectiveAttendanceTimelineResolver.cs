using HrManagement.Domain.Attendance.Corrections;
using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Application.Attendance.Corrections;

public interface IEffectiveAttendanceTimelineResolver
{
    IReadOnlyList<EffectiveAttendanceEvent> Resolve(
        Guid attendanceRecordId,
        Guid employeeId,
        IReadOnlyCollection<AttendanceEvent> rawEvents,
        IReadOnlyCollection<AttendanceCorrection> corrections);
}
