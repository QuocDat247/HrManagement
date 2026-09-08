namespace HrManagement.Application.Authorization;

public interface IAuthorizationGuard
{
    Task RequirePermissionAsync(
        string permissionCode,
        CancellationToken cancellationToken = default);
}
