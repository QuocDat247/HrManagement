using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Application.Attendance.Calculations;

public interface IAttendanceScheduleWindowResolver
{
    AttendanceScheduleWindow? Resolve(
        AttendanceRecord record);
}
