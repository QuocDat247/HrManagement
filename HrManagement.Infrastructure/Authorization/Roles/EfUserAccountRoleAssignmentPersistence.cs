using HrManagement.Application.Authorization.Roles;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Authorization.Roles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Authorization.Roles;

public sealed class EfUserAccountRoleAssignmentPersistence
    : IUserAccountRoleAssignmentPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfUserAccountRoleAssignmentPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<UserAccountRoleAssignmentPersistenceResult>
        TryReplaceAsync(
            Guid actorAccountId,
            Guid accountId,
            IReadOnlyCollection<UserAccountRole> assignments,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            assignments);

        if (actorAccountId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã tài khoản thực hiện không hợp lệ.",
                nameof(actorAccountId));
        }

        if (accountId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã tài khoản không hợp lệ.",
                nameof(accountId));
        }

        if (assignments.Any(
                assignment =>
                    assignment.AccountId !=
                    accountId))
        {
            throw new ArgumentException(
                "Danh sách vai trò chứa gán vai trò thuộc tài khoản khác.",
                nameof(assignments));
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
                    item =>
                        item.Id ==
                        actorAccountId,
                    cancellationToken);

        if (actor is null
            || !actor.IsActive)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return UserAccountRoleAssignmentPersistenceResult
                .ActorNotAuthorized;
        }

        HashSet<string>? actorPermissions =
            null;

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

            actorPermissions =
                actorPermissionCodes
                    .ToHashSet(
                        StringComparer.Ordinal);

            if (!actorPermissions.Contains(
                    PermissionCodes.AccountAssignRole))
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return UserAccountRoleAssignmentPersistenceResult
                    .ActorNotAuthorized;
            }
        }

        UserAccount? targetAccount =
            await dbContext
                .UserAccounts
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Id ==
                        accountId,
                    cancellationToken);

        if (targetAccount is null)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return UserAccountRoleAssignmentPersistenceResult
                .AccountNotFound;
        }

        if (targetAccount.Kind ==
            UserAccountKind.Owner)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return UserAccountRoleAssignmentPersistenceResult
                .OwnerAccountNotSupported;
        }

        Guid[] requestedRoleIds =
            assignments
                .Select(
                    assignment =>
                        assignment.RoleId)
                .Distinct()
                .OrderBy(
                    roleId =>
                        roleId)
                .ToArray();

        if (requestedRoleIds.Length > 0)
        {
            List<Guid> existingRequestedRoleIds =
                await dbContext
                    .Roles
                    .AsNoTracking()
                    .Where(
                        role =>
                            requestedRoleIds.Contains(
                                role.Id))
                    .Select(
                        role =>
                            role.Id)
                    .ToListAsync(
                        cancellationToken);

            if (existingRequestedRoleIds.Count !=
                requestedRoleIds.Length)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return UserAccountRoleAssignmentPersistenceResult
                    .RoleNotFound;
            }

            if (actorPermissions is not null)
            {
                List<string> requestedPermissionCodes =
                    await dbContext
                        .RolePermissions
                        .AsNoTracking()
                        .Where(
                            permission =>
                                requestedRoleIds.Contains(
                                    permission.RoleId))
                        .Select(
                            permission =>
                                permission.PermissionCode)
                        .Distinct()
                        .ToListAsync(
                            cancellationToken);

                if (requestedPermissionCodes.Any(
                        permissionCode =>
                            !actorPermissions.Contains(
                                permissionCode)))
                {
                    await transaction.RollbackAsync(
                        cancellationToken);

                    return UserAccountRoleAssignmentPersistenceResult
                        .PermissionEscalation;
                }
            }
        }

        List<UserAccountRole> existingAssignments =
            await dbContext
                .UserAccountRoles
                .Where(
                    assignment =>
                        assignment.AccountId ==
                        accountId)
                .ToListAsync(
                    cancellationToken);

        HashSet<Guid> existingRoleIds =
            existingAssignments
                .Select(
                    assignment =>
                        assignment.RoleId)
                .ToHashSet();

        HashSet<Guid> requestedRoleIdSet =
            requestedRoleIds
                .ToHashSet();

        if (existingRoleIds.SetEquals(
                requestedRoleIdSet))
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return UserAccountRoleAssignmentPersistenceResult
                .Unchanged;
        }

        UserAccountRole[] removedAssignments =
            existingAssignments
                .Where(
                    assignment =>
                        !requestedRoleIdSet.Contains(
                            assignment.RoleId))
                .ToArray();

        UserAccountRole[] addedAssignments =
            requestedRoleIds
                .Where(
                    roleId =>
                        !existingRoleIds.Contains(
                            roleId))
                .Select(
                    roleId =>
                        new UserAccountRole(
                            accountId,
                            roleId))
                .ToArray();

        dbContext.UserAccountRoles.RemoveRange(
            removedAssignments);

        await dbContext.UserAccountRoles.AddRangeAsync(
            addedAssignments,
            cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return UserAccountRoleAssignmentPersistenceResult
            .Updated;
    }
}
