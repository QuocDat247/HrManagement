namespace HrManagement.Application.Overtime.Requests;

public sealed class AuthenticatedOvertimeRequestStatusAuthorizationPolicy
    : IOvertimeRequestStatusAuthorizationPolicy
{
    public Task<bool> CanChangeStatusAsync(
        OvertimeRequestStatusAuthorizationRequest request,
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
