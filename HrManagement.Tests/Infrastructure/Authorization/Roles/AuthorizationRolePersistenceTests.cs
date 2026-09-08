using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Authorization.Roles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Infrastructure.Authorization.Roles;

public sealed class AuthorizationRolePersistenceTests
{
    [Fact]
    public async Task
        Migrations_PersistCompleteRoleAssignmentGraph()
    {
        string databasePath =
            CreateDatabasePath();

        try
        {
            var factory =
                new TestDbContextFactory(
                    databasePath);

            await MigrateAsync(
                factory);

            Guid accountId =
                Guid.NewGuid();

            Guid roleId =
                Guid.NewGuid();

            await using (
                HrManagementDbContext dbContext =
                    await factory.CreateDbContextAsync())
            {
                dbContext.UserAccounts.Add(
                    new UserAccount(
                        accountId,
                        "rbac-user",
                        "RBAC User",
                        UserAccountKind.Standard));

                dbContext.Roles.Add(
                    new Role(
                        roleId,
                        "Quản lý nhân sự",
                        "Vai trò kiểm thử."));

                dbContext.RolePermissions.Add(
                    new RolePermission(
                        roleId,
                        PermissionCodes.EmployeeView));

                dbContext.UserAccountRoles.Add(
                    new UserAccountRole(
                        accountId,
                        roleId));

                await dbContext.SaveChangesAsync();
            }

            await using (
                HrManagementDbContext dbContext =
                    await factory.CreateDbContextAsync())
            {
                Role role =
                    await dbContext.Roles
                        .AsNoTracking()
                        .SingleAsync();

                RolePermission rolePermission =
                    await dbContext.RolePermissions
                        .AsNoTracking()
                        .SingleAsync();

                UserAccountRole accountRole =
                    await dbContext.UserAccountRoles
                        .AsNoTracking()
                        .SingleAsync();

                Assert.Equal(
                    roleId,
                    role.Id);

                Assert.Equal(
                    "QUẢN LÝ NHÂN SỰ",
                    role.NormalizedName);

                Assert.Equal(
                    PermissionCodes.EmployeeView,
                    rolePermission.PermissionCode);

                Assert.Equal(
                    accountId,
                    accountRole.AccountId);

                Assert.Equal(
                    roleId,
                    accountRole.RoleId);
            }
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    [Fact]
    public async Task
        Roles_DuplicateNormalizedName_IsRejected()
    {
        string databasePath =
            CreateDatabasePath();

        try
        {
            var factory =
                new TestDbContextFactory(
                    databasePath);

            await MigrateAsync(
                factory);

            await using HrManagementDbContext dbContext =
                await factory.CreateDbContextAsync();

            dbContext.Roles.Add(
                new Role(
                    Guid.NewGuid(),
                    "Quản lý nhân sự"));

            dbContext.Roles.Add(
                new Role(
                    Guid.NewGuid(),
                    "QUẢN LÝ NHÂN SỰ"));

            await Assert.ThrowsAsync<
                DbUpdateException>(
                    () =>
                        dbContext.SaveChangesAsync());
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    private static async Task MigrateAsync(
        IDbContextFactory<HrManagementDbContext> factory)
    {
        await using HrManagementDbContext dbContext =
            await factory.CreateDbContextAsync();

        await dbContext.Database
            .MigrateAsync();
    }

    private static string CreateDatabasePath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            $"hrmanagement-rbac-{Guid.NewGuid():N}.db");
    }

    private static void DeleteDatabase(
        string databasePath)
    {
        foreach (string path in new[]
        {
            databasePath,
            $"{databasePath}-shm",
            $"{databasePath}-wal"
        })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private sealed class TestDbContextFactory
        : IDbContextFactory<HrManagementDbContext>
    {
        private readonly
            DbContextOptions<HrManagementDbContext>
                _options;

        public TestDbContextFactory(
            string databasePath)
        {
            _options =
                new DbContextOptionsBuilder<
                    HrManagementDbContext>()
                    .UseSqlite(
                        $"Data Source={databasePath};Pooling=False")
                    .Options;
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
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(
                CreateDbContext());
        }
    }
}
