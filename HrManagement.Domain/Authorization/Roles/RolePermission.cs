using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Domain.Authorization.Roles;

public sealed class RolePermission
{
    public Guid RoleId { get; }

    public string PermissionCode { get; }

    public RolePermission(
        Guid roleId,
        string permissionCode)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã vai trò không hợp lệ.",
                nameof(roleId));
        }

        var permission =
            new Permission(
                permissionCode);

        RoleId =
            roleId;

        PermissionCode =
            permission.Code;
    }
}
