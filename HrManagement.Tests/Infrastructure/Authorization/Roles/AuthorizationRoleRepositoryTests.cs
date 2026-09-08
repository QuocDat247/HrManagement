using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Authorization.Roles;
using HrManagement.Infrastructure.Authorization.Roles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Infrastructure.Authorization.Roles;

public sealed class AuthorizationRoleRepositoryTests
{
    [Fact]
    public async Task
        RoleRepository_AddAndLookupByNormalizedName_Works()
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

            var repository =
                new EfRoleRepository(
                    factory);

            Guid roleId =
                Guid.NewGuid();

            await repository.AddAsync(
                new Role(
                    roleId,
                    "Quản lý nhân sự"));

            Role? loaded =
                await repository.GetByNameAsync(
                    "  QUẢN LÝ NHÂN SỰ  ");

            Assert.NotNull(
                loaded);

            Assert.Equal(
                roleId,
                loaded.Id);
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    [Fact]
    public async Task
        RolePermissionRepository_ReplaceForRole_ReplacesAssignments()
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

            Guid roleId =
                await AddRoleAsync(
                    factory);

            var repository =
                new EfRolePermissionRepository(
                    factory);

            await repository.ReplaceForRoleAsync(
                roleId,
                new[]
                {
                    new RolePermission(
                        roleId,
                        PermissionCodes.EmployeeView),

                    new RolePermission(
                        roleId,
                        PermissionCodes.EmployeeEdit)
                });

            await repository.ReplaceForRoleAsync(
                roleId,
                new[]
                {
                    new RolePermission(
                        roleId,
                        PermissionCodes.PayrollView)
                });

            IReadOnlyList<RolePermission> loaded =
                await repository.GetByRoleIdAsync(
                    roleId);

            RolePermission permission =
                Assert.Single(
                    loaded);

            Assert.Equal(
                PermissionCodes.PayrollView,
                permission.PermissionCode);
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    [Fact]
    public async Task
        UserAccountRoleRepository_ReplaceForAccount_ReplacesAssignments()
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
                await AddAccountAsync(
                    factory);

            Guid firstRoleId =
                await AddRoleAsync(
                    factory,
                    "Vai trò một");

            Guid secondRoleId =
                await AddRoleAsync(
                    factory,
                    "Vai trò hai");

            var repository =
                new EfUserAccountRoleRepository(
                    factory);

            await repository.ReplaceForAccountAsync(
                accountId,
                new[]
                {
                    new UserAccountRole(
                        accountId,
                        firstRoleId)
                });

            await repository.ReplaceForAccountAsync(
                accountId,
                new[]
                {
                    new UserAccountRole(
                        accountId,
                        secondRoleId)
                });

            IReadOnlyList<UserAccountRole> loaded =
                await repository.GetByAccountIdAsync(
                    accountId);

            UserAccountRole assignment =
                Assert.Single(
                    loaded);

            Assert.Equal(
                secondRoleId,
                assignment.RoleId);
        }
        finally
        {
            DeleteDatabase(
                databasePath);
        }
    }

    private static async Task<Guid> AddAccountAsync(
        IDbContextFactory<HrManagementDbContext> factory)
    {
        Guid accountId =
            Guid.NewGuid();

        await using HrManagementDbContext dbContext =
            await factory.CreateDbContextAsync();

        dbContext.UserAccounts.Add(
            new UserAccount(
                accountId,
                $"user-{accountId:N}",
                "Người dùng kiểm thử",
                UserAccountKind.Standard));

        await dbContext.SaveChangesAsync();

        return accountId;
    }

    private static async Task<Guid> AddRoleAsync(
        IDbContextFactory<HrManagementDbContext> factory,
        string? name = null)
    {
        Guid roleId =
            Guid.NewGuid();

        await using HrManagementDbContext dbContext =
            await factory.CreateDbContextAsync();

        dbContext.Roles.Add(
            new Role(
                roleId,
                name
                ?? $"role-{roleId:N}"));

        await dbContext.SaveChangesAsync();

        return roleId;
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
            $"hrmanagement-rbac-repositories-{Guid.NewGuid():N}.db");
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
