using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Application.Attendance.Schedules;

public interface IEmployeeWorkScheduleAssignmentRepository
{
    Task<IReadOnlyList<EmployeeWorkScheduleAssignment>>
        GetByEmployeeIdAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);
}
