namespace HrManagement.Application.Attendance.Corrections;

public sealed class AuthenticatedAttendanceCorrectionAuthorizationPolicy
    : IAttendanceCorrectionAuthorizationPolicy
{
    public Task<bool> CanApplyAsync(
        AttendanceCorrectionAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        return Task.FromResult(
            !string.IsNullOrWhiteSpace(
                request.Actor.UserId)
            && !string.IsNullOrWhiteSpace(
                request.Actor.Username));
    }
}
