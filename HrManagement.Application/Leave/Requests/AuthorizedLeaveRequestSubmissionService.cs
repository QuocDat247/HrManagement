using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Application.Leave.Requests;

public sealed class AuthorizedLeaveRequestSubmissionService
    : ILeaveRequestSubmissionService
{
    private readonly ILeaveRequestSubmissionService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedLeaveRequestSubmissionService(
        ILeaveRequestSubmissionService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<SubmitLeaveRequestResult> SubmitAsync(
        SubmitLeaveRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        await _authorizationGuard
            .RequirePermissionAsync(
                PermissionCodes.LeaveSubmit,
                cancellationToken);

        return await _inner
            .SubmitAsync(
                request,
                cancellationToken);
    }
}
