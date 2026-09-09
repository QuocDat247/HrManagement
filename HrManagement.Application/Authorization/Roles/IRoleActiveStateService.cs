namespace HrManagement.Application.Authorization.Roles;

public interface IRoleActiveStateService
{
    Task<RoleManagementResult> SetAsync(
        Guid roleId,
        bool isActive,
        CancellationToken cancellationToken = default);
}
