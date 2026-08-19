using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Application.Attendance.Schedules;

public interface IEmployeeWorkScheduleAssignmentPersistence
{
    Task ApplyAsync(
        EmployeeWorkScheduleAssignment? closedAssignment,
        EmployeeWorkScheduleAssignment newAssignment,
        CancellationToken cancellationToken = default);
}
