namespace HrManagement.Application.Overtime.Requests;

public interface IOvertimeRequestStatusAuthorizationPolicy
{
    Task<bool> CanChangeStatusAsync(
        OvertimeRequestStatusAuthorizationRequest request,
        CancellationToken cancellationToken = default);
}
