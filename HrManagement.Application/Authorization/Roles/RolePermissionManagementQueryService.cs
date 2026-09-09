using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Authorization.Roles;

namespace HrManagement.Application.Authorization.Roles;

public sealed class RolePermissionManagementQueryService
    : IRolePermissionManagementQueryService
{
    private readonly IRoleRepository
        _roleRepository;

    private readonly IRolePermissionRepository
        _rolePermissionRepository;

    public RolePermissionManagementQueryService(
        IRoleRepository roleRepository,
        IRolePermissionRepository rolePermissionRepository)
    {
        _roleRepository =
            roleRepository;

        _rolePermissionRepository =
            rolePermissionRepository;
    }

    public async Task<RolePermissionManagementSnapshot?> GetAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        if (roleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã vai trò không hợp lệ.",
                nameof(roleId));
        }

        Role? role =
            await _roleRepository
                .GetByIdAsync(
                    roleId,
                    cancellationToken);

        if (role is null)
        {
            return null;
        }

        IReadOnlyList<RolePermission> assignments =
            await _rolePermissionRepository
                .GetByRoleIdAsync(
                    roleId,
                    cancellationToken);

        foreach (RolePermission assignment
                 in assignments)
        {
            if (assignment.RoleId !=
                    roleId
                || !PermissionCodes.All.Contains(
                    assignment.PermissionCode))
            {
                throw new InvalidOperationException(
                    "Dữ liệu quyền của vai trò không nhất quán.");
            }
        }

        string[] availablePermissionCodes =
            PermissionCodes.All
                .OrderBy(
                    code =>
                        code,
                    StringComparer.Ordinal)
                .ToArray();

        string[] assignedPermissionCodes =
            assignments
                .Select(
                    assignment =>
                        assignment.PermissionCode)
                .Distinct(
                    StringComparer.Ordinal)
                .OrderBy(
                    code =>
                        code,
                    StringComparer.Ordinal)
                .ToArray();

        return new RolePermissionManagementSnapshot(
            role.Id,
            role.Name,
            role.IsActive,
            availablePermissionCodes,
            assignedPermissionCodes);
    }
}
