namespace HrManagement.Application.Attendance.Records;

public interface IAttendancePunchService
{
    Task<RecordAttendancePunchResult> RecordAsync(
        RecordAttendancePunchRequest request,
        CancellationToken cancellationToken = default);
}
