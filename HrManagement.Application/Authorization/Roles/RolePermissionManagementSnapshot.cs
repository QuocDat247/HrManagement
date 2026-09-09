namespace HrManagement.Application.Authorization.Roles;

public sealed record RolePermissionManagementSnapshot(
    Guid RoleId,
    string RoleName,
    bool IsActive,
    IReadOnlyList<string> AvailablePermissionCodes,
    IReadOnlyList<string> AssignedPermissionCodes);
