namespace HrManagement.Application.Authorization;

public interface IAuthorizationService
{
    Task<bool> HasPermissionAsync(
        string permissionCode,
        CancellationToken cancellationToken = default);
}
