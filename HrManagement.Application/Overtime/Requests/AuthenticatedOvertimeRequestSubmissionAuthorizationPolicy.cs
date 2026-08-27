namespace HrManagement.Application.Overtime.Requests;

public sealed class AuthenticatedOvertimeRequestSubmissionAuthorizationPolicy
    : IOvertimeRequestSubmissionAuthorizationPolicy
{
    public Task<bool> CanSubmitAsync(
        OvertimeRequestSubmissionAuthorizationRequest request,
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
