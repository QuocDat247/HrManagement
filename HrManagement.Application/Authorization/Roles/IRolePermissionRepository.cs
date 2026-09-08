using HrManagement.Domain.Authorization.Roles;

namespace HrManagement.Application.Authorization.Roles;

public interface IRolePermissionRepository
{
    Task<IReadOnlyList<RolePermission>> GetByRoleIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task ReplaceForRoleAsync(
        Guid roleId,
        IReadOnlyCollection<RolePermission> permissions,
        CancellationToken cancellationToken = default);
}
