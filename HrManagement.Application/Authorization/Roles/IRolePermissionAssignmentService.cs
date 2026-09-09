namespace HrManagement.Application.Authorization.Roles;

public interface IRolePermissionAssignmentService
{
    Task<RoleManagementResult> ReplaceAsync(
        Guid roleId,
        IReadOnlyCollection<string> permissionCodes,
        CancellationToken cancellationToken = default);
}
