namespace HrManagement.Application.Attendance.Calculations;

public interface IApprovedLeaveAttendanceResolver
{
    Task<ApprovedLeaveAttendanceInput?> ResolveAsync(
        Guid employeeId,
        Guid employmentPeriodId,
        DateOnly workDate,
        CancellationToken cancellationToken = default);
}
