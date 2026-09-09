namespace HrManagement.Application.Authorization.Roles;

public interface IRoleManagementService
{
    Task<RoleManagementResult> CreateAsync(
        string name,
        string? description = null,
        CancellationToken cancellationToken = default);

    Task<RoleManagementResult> UpdateAsync(
        Guid roleId,
        string name,
        string? description = null,
        CancellationToken cancellationToken = default);
}
