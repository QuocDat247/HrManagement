namespace HrManagement.Application.Authorization.Roles;

public interface IRoleActiveStatePersistence
{
    Task<RoleActiveStatePersistenceResult> TrySetAsync(
        Guid roleId,
        bool isActive,
        CancellationToken cancellationToken = default);
}

public enum RoleActiveStatePersistenceResult
{
    Updated = 0,
    Unchanged = 1,
    RoleNotFound = 2
}
