namespace HrManagement.Application.Attendance.Corrections;

public interface IAttendanceCorrectionAuthorizationPolicy
{
    Task<bool> CanApplyAsync(
        AttendanceCorrectionAuthorizationRequest request,
        CancellationToken cancellationToken = default);
}
