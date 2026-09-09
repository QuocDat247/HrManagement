using HrManagement.Application.Authorization.Roles;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Authorization.Roles;
using HrManagement.Infrastructure.Authorization.Roles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Persistence;

public sealed class EfRolePermissionAssignmentPersistenceTests
{
    [Fact]
    public async Task
        TryReplaceAsync_OwnerCanAssignAnyKnownPermission()
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

        Guid targetRoleId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(
                         options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.UserAccounts.Add(
                new UserAccount(
                    ownerId,
                    "owner",
                    "Owner",
                    UserAccountKind.Owner));

            dbContext.Roles.Add(
                new Role(
                    targetRoleId,
                    "Quản lý"));

            dbContext.RolePermissions.Add(
                new RolePermission(
                    targetRoleId,
                    PermissionCodes.EmployeeView));

            await dbContext.SaveChangesAsync();
        }

        var persistence =
            new EfRolePermissionAssignmentPersistence(
                new TestDbContextFactory(
                    options));

        RolePermissionAssignmentPersistenceResult result =
            await persistence.TryReplaceAsync(
                ownerId,
                targetRoleId,
                new[]
                {
                    new RolePermission(
                        targetRoleId,
                        PermissionCodes.PayrollClose),
                    new RolePermission(
                        targetRoleId,
                        PermissionCodes.AccountCreate)
                });

        Assert.Equal(
            RolePermissionAssignmentPersistenceResult.Updated,
            result);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        string[] permissions =
            await verificationContext
                .RolePermissions
                .Where(
                    permission =>
                        permission.RoleId ==
                        targetRoleId)
                .OrderBy(
                    permission =>
                        permission.PermissionCode)
                .Select(
                    permission =>
                        permission.PermissionCode)
                .ToArrayAsync();

        Assert.Equal(
            new[]
            {
                PermissionCodes.AccountCreate,
                PermissionCodes.PayrollClose
            },
            permissions);
    }

    [Fact]
    public async Task
        TryReplaceAsync_StandardCannotGrantPermissionItDoesNotHave()
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

            dbContext.UserAccounts.Add(
                new UserAccount(
                    actorId,
                    "manager",
                    "Manager",
                    UserAccountKind.Standard));

            dbContext.Roles.AddRange(
                new Role(
                    actorRoleId,
                    "Role Manager"),
                new Role(
                    targetRoleId,
                    "Target"));

            dbContext.UserAccountRoles.Add(
                new UserAccountRole(
                    actorId,
                    actorRoleId));

            dbContext.RolePermissions.AddRange(
                new RolePermission(
                    actorRoleId,
                    PermissionCodes.RoleAssignPermission),
                new RolePermission(
                    actorRoleId,
                    PermissionCodes.EmployeeView),
                new RolePermission(
                    targetRoleId,
                    PermissionCodes.EmployeeView));

            await dbContext.SaveChangesAsync();
        }

        var persistence =
            new EfRolePermissionAssignmentPersistence(
                new TestDbContextFactory(
                    options));

        RolePermissionAssignmentPersistenceResult result =
            await persistence.TryReplaceAsync(
                actorId,
                targetRoleId,
                new[]
                {
                    new RolePermission(
                        targetRoleId,
                        PermissionCodes.EmployeeView),
                    new RolePermission(
                        targetRoleId,
                        PermissionCodes.PayrollClose)
                });

        Assert.Equal(
            RolePermissionAssignmentPersistenceResult
                .PermissionEscalation,
            result);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        string[] permissions =
            await verificationContext
                .RolePermissions
                .Where(
                    permission =>
                        permission.RoleId ==
                        targetRoleId)
                .Select(
                    permission =>
                        permission.PermissionCode)
                .ToArrayAsync();

        Assert.Single(
            permissions);

        Assert.Equal(
            PermissionCodes.EmployeeView,
            permissions[0]);
    }

    [Fact]
    public async Task
        TryReplaceAsync_StandardCanAssignSubsetOfOwnPermissions()
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

            dbContext.UserAccounts.Add(
                new UserAccount(
                    actorId,
                    "manager",
                    "Manager",
                    UserAccountKind.Standard));

            dbContext.Roles.AddRange(
                new Role(
                    actorRoleId,
                    "Role Manager"),
                new Role(
                    targetRoleId,
                    "Target",
                    isActive:
                        false));

            dbContext.UserAccountRoles.Add(
                new UserAccountRole(
                    actorId,
                    actorRoleId));

            dbContext.RolePermissions.AddRange(
                new RolePermission(
                    actorRoleId,
                    PermissionCodes.RoleAssignPermission),
                new RolePermission(
                    actorRoleId,
                    PermissionCodes.EmployeeView),
                new RolePermission(
                    actorRoleId,
                    PermissionCodes.EmployeeEdit),
                new RolePermission(
                    targetRoleId,
                    PermissionCodes.PayrollView));

            await dbContext.SaveChangesAsync();
        }

        var persistence =
            new EfRolePermissionAssignmentPersistence(
                new TestDbContextFactory(
                    options));

        RolePermissionAssignmentPersistenceResult result =
            await persistence.TryReplaceAsync(
                actorId,
                targetRoleId,
                new[]
                {
                    new RolePermission(
                        targetRoleId,
                        PermissionCodes.EmployeeView),
                    new RolePermission(
                        targetRoleId,
                        PermissionCodes.EmployeeEdit)
                });

        Assert.Equal(
            RolePermissionAssignmentPersistenceResult.Updated,
            result);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        string[] permissions =
            await verificationContext
                .RolePermissions
                .Where(
                    permission =>
                        permission.RoleId ==
                        targetRoleId)
                .OrderBy(
                    permission =>
                        permission.PermissionCode)
                .Select(
                    permission =>
                        permission.PermissionCode)
                .ToArrayAsync();

        Assert.Equal(
            new[]
            {
                PermissionCodes.EmployeeEdit,
                PermissionCodes.EmployeeView
            },
            permissions);
    }

    [Fact]
    public async Task
        TryReplaceAsync_InactiveActorRoleDoesNotAuthorizeAssignment()
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

            dbContext.UserAccounts.Add(
                new UserAccount(
                    actorId,
                    "manager",
                    "Manager",
                    UserAccountKind.Standard));

            dbContext.Roles.AddRange(
                new Role(
                    actorRoleId,
                    "Inactive Manager",
                    isActive:
                        false),
                new Role(
                    targetRoleId,
                    "Target"));

            dbContext.UserAccountRoles.Add(
                new UserAccountRole(
                    actorId,
                    actorRoleId));

            dbContext.RolePermissions.AddRange(
                new RolePermission(
                    actorRoleId,
                    PermissionCodes.RoleAssignPermission),
                new RolePermission(
                    actorRoleId,
                    PermissionCodes.EmployeeView));

            await dbContext.SaveChangesAsync();
        }

        var persistence =
            new EfRolePermissionAssignmentPersistence(
                new TestDbContextFactory(
                    options));

        RolePermissionAssignmentPersistenceResult result =
            await persistence.TryReplaceAsync(
                actorId,
                targetRoleId,
                new[]
                {
                    new RolePermission(
                        targetRoleId,
                        PermissionCodes.EmployeeView)
                });

        Assert.Equal(
            RolePermissionAssignmentPersistenceResult
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
