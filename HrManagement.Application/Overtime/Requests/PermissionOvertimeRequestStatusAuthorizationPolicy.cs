using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Application.Overtime.Requests;

public sealed class PermissionOvertimeRequestStatusAuthorizationPolicy
    : IOvertimeRequestStatusAuthorizationPolicy
{
    private readonly IAuthorizationService
        _authorizationService;

    public PermissionOvertimeRequestStatusAuthorizationPolicy(
        IAuthorizationService authorizationService)
    {
        _authorizationService =
            authorizationService;
    }

    public Task<bool> CanChangeStatusAsync(
        OvertimeRequestStatusAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        cancellationToken
            .ThrowIfCancellationRequested();

        string? permissionCode =
            request.TargetStatus switch
            {
                OvertimeRequestStatus.Approved =>
                    PermissionCodes.OvertimeReview,

                OvertimeRequestStatus.Rejected =>
                    PermissionCodes.OvertimeReview,

                OvertimeRequestStatus.Cancelled =>
                    PermissionCodes.OvertimeCancel,

                _ =>
                    null
            };

        if (permissionCode is null)
        {
            return Task.FromResult(
                false);
        }

        return _authorizationService
            .HasPermissionAsync(
                permissionCode,
                cancellationToken);
    }
}
