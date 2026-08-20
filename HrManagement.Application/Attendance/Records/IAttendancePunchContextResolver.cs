namespace HrManagement.Application.Attendance.Records;

public interface IAttendancePunchContextResolver
{
    Task<AttendancePunchContextResolutionResult> ResolveAsync(
        Guid employeeId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);
}
