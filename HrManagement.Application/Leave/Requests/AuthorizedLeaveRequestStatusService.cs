using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Leave.Requests;

namespace HrManagement.Application.Leave.Requests;

public sealed class AuthorizedLeaveRequestStatusService
    : ILeaveRequestStatusService
{
    private readonly ILeaveRequestStatusService
        _inner;

    private readonly IAuthorizationGuard
        _authorizationGuard;

    public AuthorizedLeaveRequestStatusService(
        ILeaveRequestStatusService inner,
        IAuthorizationGuard authorizationGuard)
    {
        _inner =
            inner;

        _authorizationGuard =
            authorizationGuard;
    }

    public async Task<ChangeLeaveRequestStatusResult>
        ChangeStatusAsync(
            ChangeLeaveRequestStatusRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        string? permissionCode =
            request.TargetStatus switch
            {
                LeaveRequestStatus.Approved =>
                    PermissionCodes.LeaveApprove,

                LeaveRequestStatus.Rejected =>
                    PermissionCodes.LeaveApprove,

                LeaveRequestStatus.Cancelled =>
                    PermissionCodes.LeaveCancel,

                _ =>
                    null
            };

        if (permissionCode is not null)
        {
            await _authorizationGuard
                .RequirePermissionAsync(
                    permissionCode,
                    cancellationToken);
        }

        return await _inner
            .ChangeStatusAsync(
                request,
                cancellationToken);
    }
}
