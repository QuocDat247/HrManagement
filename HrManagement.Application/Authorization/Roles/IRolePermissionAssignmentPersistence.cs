using HrManagement.Domain.Authorization.Roles;

namespace HrManagement.Application.Authorization.Roles;

public interface IRolePermissionAssignmentPersistence
{
    Task<RolePermissionAssignmentPersistenceResult>
        TryReplaceAsync(
            Guid actorAccountId,
            Guid roleId,
            IReadOnlyCollection<RolePermission> permissions,
            CancellationToken cancellationToken = default);
}

public enum RolePermissionAssignmentPersistenceResult
{
    Updated = 0,
    Unchanged = 1,
    RoleNotFound = 2,
    ActorNotAuthorized = 3,
    PermissionEscalation = 4
}
