namespace HrManagement.Application.Authorization.Roles;

public interface IRolePermissionManagementQueryService
{
    Task<RolePermissionManagementSnapshot?> GetAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);
}
