using HrManagement.Domain.Authorization.Roles;

namespace HrManagement.Application.Authorization.Roles;

public interface IRoleManagementPersistence
{
    Task<RoleManagementPersistenceResult> TryCreateAsync(
        Role role,
        CancellationToken cancellationToken = default);

    Task<RoleManagementPersistenceResult> TryUpdateAsync(
        Guid roleId,
        string name,
        string? description,
        CancellationToken cancellationToken = default);
}

public enum RoleManagementPersistenceResult
{
    Created = 0,
    Updated = 1,
    RoleNotFound = 2,
    NameAlreadyExists = 3
}
