using HrManagement.Application.Authorization.Roles;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Authorization.Roles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Authorization.Roles;

public sealed class EfRolePermissionAssignmentPersistence
    : IRolePermissionAssignmentPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfRolePermissionAssignmentPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<RolePermissionAssignmentPersistenceResult>
        TryReplaceAsync(
            Guid actorAccountId,
            Guid roleId,
            IReadOnlyCollection<RolePermission> permissions,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            permissions);

        if (actorAccountId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã tài khoản thực hiện không hợp lệ.",
                nameof(actorAccountId));
        }

        if (roleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã vai trò không hợp lệ.",
                nameof(roleId));
        }

        if (permissions.Any(
                permission =>
                    permission.RoleId !=
                    roleId))
        {
            throw new ArgumentException(
                "Danh sách quyền chứa quyền thuộc vai trò khác.",
                nameof(permissions));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        var connection =
            (SqliteConnection)
                dbContext.Database
                    .GetDbConnection();

        await connection.OpenAsync(
            cancellationToken);

        await using SqliteTransaction transaction =
            connection.BeginTransaction(
                deferred:
                    false);

        await dbContext.Database
            .UseTransactionAsync(
                transaction,
                cancellationToken);

        UserAccount? actor =
            await dbContext
                .UserAccounts
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    account =>
                        account.Id ==
                        actorAccountId,
                    cancellationToken);

        if (actor is null
            || !actor.IsActive)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return RolePermissionAssignmentPersistenceResult
                .ActorNotAuthorized;
        }

        bool roleExists =
            await dbContext
                .Roles
                .AsNoTracking()
                .AnyAsync(
                    role =>
                        role.Id ==
                        roleId,
                    cancellationToken);

        if (!roleExists)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return RolePermissionAssignmentPersistenceResult
                .RoleNotFound;
        }

        if (actor.Kind !=
            UserAccountKind.Owner)
        {
            List<string> actorPermissionCodes =
                await (
                    from assignment in
                        dbContext.UserAccountRoles
                            .AsNoTracking()
                    join role in
                        dbContext.Roles
                            .AsNoTracking()
                        on assignment.RoleId
                        equals role.Id
                    join permission in
                        dbContext.RolePermissions
                            .AsNoTracking()
                        on role.Id
                        equals permission.RoleId
                    where assignment.AccountId ==
                            actorAccountId
                        && role.IsActive
                    select permission.PermissionCode)
                    .Distinct()
                    .ToListAsync(
                        cancellationToken);

            HashSet<string> actorPermissions =
                actorPermissionCodes
                    .ToHashSet(
                        StringComparer.Ordinal);

            if (!actorPermissions.Contains(
                    PermissionCodes.RoleAssignPermission))
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return RolePermissionAssignmentPersistenceResult
                    .ActorNotAuthorized;
            }

            if (permissions.Any(
                    permission =>
                        !actorPermissions.Contains(
                            permission.PermissionCode)))
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return RolePermissionAssignmentPersistenceResult
                    .PermissionEscalation;
            }
        }

        List<RolePermission> existingPermissions =
            await dbContext
                .RolePermissions
                .Where(
                    permission =>
                        permission.RoleId ==
                        roleId)
                .ToListAsync(
                    cancellationToken);

        HashSet<string> existingCodes =
            existingPermissions
                .Select(
                    permission =>
                        permission.PermissionCode)
                .ToHashSet(
                    StringComparer.Ordinal);

        HashSet<string> requestedCodes =
            permissions
                .Select(
                    permission =>
                        permission.PermissionCode)
                .ToHashSet(
                    StringComparer.Ordinal);

        if (existingCodes.SetEquals(
                requestedCodes))
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return RolePermissionAssignmentPersistenceResult
                .Unchanged;
        }

        RolePermission[] removedPermissions =
            existingPermissions
                .Where(
                    permission =>
                        !requestedCodes.Contains(
                            permission.PermissionCode))
                .ToArray();

        RolePermission[] addedPermissions =
            permissions
                .Where(
                    permission =>
                        !existingCodes.Contains(
                            permission.PermissionCode))
                .ToArray();

        dbContext.RolePermissions.RemoveRange(
            removedPermissions);

        await dbContext.RolePermissions.AddRangeAsync(
            addedPermissions,
            cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return RolePermissionAssignmentPersistenceResult
            .Updated;
    }
}
