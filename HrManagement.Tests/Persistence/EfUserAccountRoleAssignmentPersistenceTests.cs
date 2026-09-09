using HrManagement.Application.Authorization.Roles;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Authorization.Roles;
using HrManagement.Infrastructure.Authorization.Roles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Persistence;

public sealed class EfUserAccountRoleAssignmentPersistenceTests
{
    [Fact]
    public async Task
        TryReplaceAsync_OwnerCanAssignRolesToStandardAccount()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        Guid ownerId =
            Guid.NewGuid();

        Guid accountId =
            Guid.NewGuid();

        Guid roleId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(
                         options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.UserAccounts.AddRange(
                new UserAccount(
                    ownerId,
                    "owner",
                    "Owner",
                    UserAccountKind.Owner),
                new UserAccount(
                    accountId,
                    "manager",
                    "Manager",
                    UserAccountKind.Standard));

            dbContext.Roles.Add(
                new Role(
                    roleId,
                    "Payroll Manager"));

            dbContext.RolePermissions.Add(
                new RolePermission(
                    roleId,
                    PermissionCodes.PayrollClose));

            await dbContext.SaveChangesAsync();
        }

        var persistence =
            new EfUserAccountRoleAssignmentPersistence(
                new TestDbContextFactory(
                    options));

        UserAccountRoleAssignmentPersistenceResult result =
            await persistence.TryReplaceAsync(
                ownerId,
                accountId,
                new[]
                {
                    new UserAccountRole(
                        accountId,
                        roleId)
                });

        Assert.Equal(
            UserAccountRoleAssignmentPersistenceResult.Updated,
            result);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        UserAccountRole saved =
            await verificationContext
                .UserAccountRoles
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            accountId,
            saved.AccountId);

        Assert.Equal(
            roleId,
            saved.RoleId);
    }

    [Fact]
    public async Task
        TryReplaceAsync_CannotAssignRolesToOwner()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        Guid actorOwnerId =
            Guid.NewGuid();

        Guid targetOwnerId =
            Guid.NewGuid();

        Guid roleId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(
                         options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.UserAccounts.AddRange(
                new UserAccount(
                    actorOwnerId,
                    "owner.one",
                    "Owner One",
                    UserAccountKind.Owner),
                new UserAccount(
                    targetOwnerId,
                    "owner.two",
                    "Owner Two",
                    UserAccountKind.Owner));

            dbContext.Roles.Add(
                new Role(
                    roleId,
                    "Manager"));

            await dbContext.SaveChangesAsync();
        }

        var persistence =
            new EfUserAccountRoleAssignmentPersistence(
                new TestDbContextFactory(
                    options));

        UserAccountRoleAssignmentPersistenceResult result =
            await persistence.TryReplaceAsync(
                actorOwnerId,
                targetOwnerId,
                new[]
                {
                    new UserAccountRole(
                        targetOwnerId,
                        roleId)
                });

        Assert.Equal(
            UserAccountRoleAssignmentPersistenceResult
                .OwnerAccountNotSupported,
            result);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        Assert.Empty(
            await verificationContext
                .UserAccountRoles
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task
        TryReplaceAsync_StandardCannotAssignInactiveRoleWithExtraPermission()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        Guid actorId =
            Guid.NewGuid();

        Guid targetAccountId =
            Guid.NewGuid();

        Guid actorRoleId =
            Guid.NewGuid();

        Guid targetRoleId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(
                         options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.UserAccounts.AddRange(
                new UserAccount(
                    actorId,
                    "manager",
                    "Manager",
                    UserAccountKind.Standard),
                new UserAccount(
                    targetAccountId,
                    "target",
                    "Target",
                    UserAccountKind.Standard));

            dbContext.Roles.AddRange(
                new Role(
                    actorRoleId,
                    "Account Manager"),
                new Role(
                    targetRoleId,
                    "Inactive Payroll",
                    isActive:
                        false));

            dbContext.UserAccountRoles.Add(
                new UserAccountRole(
                    actorId,
                    actorRoleId));

            dbContext.RolePermissions.AddRange(
                new RolePermission(
                    actorRoleId,
                    PermissionCodes.AccountAssignRole),
                new RolePermission(
                    actorRoleId,
                    PermissionCodes.EmployeeView),
                new RolePermission(
                    targetRoleId,
                    PermissionCodes.PayrollClose));

            await dbContext.SaveChangesAsync();
        }

        var persistence =
            new EfUserAccountRoleAssignmentPersistence(
                new TestDbContextFactory(
                    options));

        UserAccountRoleAssignmentPersistenceResult result =
            await persistence.TryReplaceAsync(
                actorId,
                targetAccountId,
                new[]
                {
                    new UserAccountRole(
                        targetAccountId,
                        targetRoleId)
                });

        Assert.Equal(
            UserAccountRoleAssignmentPersistenceResult
                .PermissionEscalation,
            result);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        Assert.DoesNotContain(
            await verificationContext
                .UserAccountRoles
                .AsNoTracking()
                .ToListAsync(),
            assignment =>
                assignment.AccountId ==
                targetAccountId);
    }

    [Fact]
    public async Task
        TryReplaceAsync_StandardCanAssignRoleWithinOwnPermissions()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        Guid actorId =
            Guid.NewGuid();

        Guid targetAccountId =
            Guid.NewGuid();

        Guid actorRoleId =
            Guid.NewGuid();

        Guid targetRoleId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(
                         options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.UserAccounts.AddRange(
                new UserAccount(
                    actorId,
                    "manager",
                    "Manager",
                    UserAccountKind.Standard),
                new UserAccount(
                    targetAccountId,
                    "target",
                    "Target",
                    UserAccountKind.Standard));

            dbContext.Roles.AddRange(
                new Role(
                    actorRoleId,
                    "Account Manager"),
                new Role(
                    targetRoleId,
                    "Employee Viewer",
                    isActive:
                        false));

            dbContext.UserAccountRoles.Add(
                new UserAccountRole(
                    actorId,
                    actorRoleId));

            dbContext.RolePermissions.AddRange(
                new RolePermission(
                    actorRoleId,
                    PermissionCodes.AccountAssignRole),
                new RolePermission(
                    actorRoleId,
                    PermissionCodes.EmployeeView),
                new RolePermission(
                    targetRoleId,
                    PermissionCodes.EmployeeView));

            await dbContext.SaveChangesAsync();
        }

        var persistence =
            new EfUserAccountRoleAssignmentPersistence(
                new TestDbContextFactory(
                    options));

        UserAccountRoleAssignmentPersistenceResult result =
            await persistence.TryReplaceAsync(
                actorId,
                targetAccountId,
                new[]
                {
                    new UserAccountRole(
                        targetAccountId,
                        targetRoleId)
                });

        Assert.Equal(
            UserAccountRoleAssignmentPersistenceResult.Updated,
            result);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        UserAccountRole saved =
            await verificationContext
                .UserAccountRoles
                .AsNoTracking()
                .SingleAsync(
                    assignment =>
                        assignment.AccountId ==
                        targetAccountId);

        Assert.Equal(
            targetRoleId,
            saved.RoleId);
    }

    [Fact]
    public async Task
        TryReplaceAsync_StandardWithoutAssignRolePermission_IsRejected()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        Guid actorId =
            Guid.NewGuid();

        Guid targetAccountId =
            Guid.NewGuid();

        Guid actorRoleId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(
                         options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.UserAccounts.AddRange(
                new UserAccount(
                    actorId,
                    "manager",
                    "Manager",
                    UserAccountKind.Standard),
                new UserAccount(
                    targetAccountId,
                    "target",
                    "Target",
                    UserAccountKind.Standard));

            dbContext.Roles.Add(
                new Role(
                    actorRoleId,
                    "Employee Viewer"));

            dbContext.UserAccountRoles.Add(
                new UserAccountRole(
                    actorId,
                    actorRoleId));

            dbContext.RolePermissions.Add(
                new RolePermission(
                    actorRoleId,
                    PermissionCodes.EmployeeView));

            await dbContext.SaveChangesAsync();
        }

        var persistence =
            new EfUserAccountRoleAssignmentPersistence(
                new TestDbContextFactory(
                    options));

        UserAccountRoleAssignmentPersistenceResult result =
            await persistence.TryReplaceAsync(
                actorId,
                targetAccountId,
                Array.Empty<UserAccountRole>());

        Assert.Equal(
            UserAccountRoleAssignmentPersistenceResult
                .ActorNotAuthorized,
            result);
    }

    private static DbContextOptions<HrManagementDbContext>
        CreateOptions(
            SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<
                HrManagementDbContext>()
            .UseSqlite(
                connection)
            .Options;
    }

    private sealed class TestDbContextFactory
        : IDbContextFactory<HrManagementDbContext>
    {
        private readonly DbContextOptions<HrManagementDbContext>
            _options;

        public TestDbContextFactory(
            DbContextOptions<HrManagementDbContext> options)
        {
            _options =
                options;
        }

        public HrManagementDbContext CreateDbContext()
        {
            return new HrManagementDbContext(
                _options);
        }

        public Task<HrManagementDbContext>
            CreateDbContextAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new HrManagementDbContext(
                    _options));
        }
    }
}
