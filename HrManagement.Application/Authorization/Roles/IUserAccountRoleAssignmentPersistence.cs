using HrManagement.Domain.Authorization.Roles;

namespace HrManagement.Application.Authorization.Roles;

public interface IUserAccountRoleAssignmentPersistence
{
    Task<UserAccountRoleAssignmentPersistenceResult>
        TryReplaceAsync(
            Guid actorAccountId,
            Guid accountId,
            IReadOnlyCollection<UserAccountRole> assignments,
            CancellationToken cancellationToken = default);
}

public enum UserAccountRoleAssignmentPersistenceResult
{
    Updated = 0,
    Unchanged = 1,
    AccountNotFound = 2,
    OwnerAccountNotSupported = 3,
    RoleNotFound = 4,
    ActorNotAuthorized = 5,
    PermissionEscalation = 6
}
