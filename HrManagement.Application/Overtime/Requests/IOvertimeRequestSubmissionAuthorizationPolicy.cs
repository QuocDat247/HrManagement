namespace HrManagement.Application.Overtime.Requests;

public interface IOvertimeRequestSubmissionAuthorizationPolicy
{
    Task<bool> CanSubmitAsync(
        OvertimeRequestSubmissionAuthorizationRequest request,
        CancellationToken cancellationToken = default);
}
