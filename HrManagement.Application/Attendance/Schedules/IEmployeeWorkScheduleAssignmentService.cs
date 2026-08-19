namespace HrManagement.Application.Attendance.Schedules;

public interface IEmployeeWorkScheduleAssignmentService
{
    Task<AssignEmployeeWorkScheduleResult> AssignAsync(
        AssignEmployeeWorkScheduleRequest request,
        CancellationToken cancellationToken = default);
}
