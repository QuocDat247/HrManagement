using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Overtime.Requests;

public sealed class PermissionOvertimeRequestSubmissionAuthorizationPolicy
    : IOvertimeRequestSubmissionAuthorizationPolicy
{
    private readonly IAuthorizationService
        _authorizationService;

    public PermissionOvertimeRequestSubmissionAuthorizationPolicy(
        IAuthorizationService authorizationService)
    {
        _authorizationService =
            authorizationService;
    }

    public Task<bool> CanSubmitAsync(
        OvertimeRequestSubmissionAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        return _authorizationService
            .HasPermissionAsync(
                PermissionCodes.OvertimeSubmit,
                cancellationToken);
    }
}
