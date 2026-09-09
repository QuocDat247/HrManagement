using HrManagement.Application.Authorization.Roles;
using HrManagement.Domain.Authorization.Roles;
using HrManagement.Infrastructure.Authorization.Roles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Persistence;

public sealed class EfRoleActiveStatePersistenceTests
{
    [Fact]
    public async Task
        TrySetAsync_CanDisableRoleWithoutChangingDetails()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        Guid roleId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(
                         options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Roles.Add(
                new Role(
                    roleId,
                    "Quản lý nhân sự",
                    "Quản lý hồ sơ."));

            await dbContext.SaveChangesAsync();
        }

        var persistence =
            new EfRoleActiveStatePersistence(
                new TestDbContextFactory(
                    options));

        RoleActiveStatePersistenceResult result =
            await persistence.TrySetAsync(
                roleId,
                false);

        Assert.Equal(
            RoleActiveStatePersistenceResult.Updated,
            result);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        Role saved =
            await verificationContext
                .Roles
                .AsNoTracking()
                .SingleAsync(
                    role =>
                        role.Id ==
                        roleId);

        Assert.False(
            saved.IsActive);

        Assert.Equal(
            "Quản lý nhân sự",
            saved.Name);

        Assert.Equal(
            "QUẢN LÝ NHÂN SỰ",
            saved.NormalizedName);

        Assert.Equal(
            "Quản lý hồ sơ.",
            saved.Description);
    }

    [Fact]
    public async Task
        TrySetAsync_WhenStateAlreadyMatches_ReturnsUnchanged()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        Guid roleId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(
                         options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Roles.Add(
                new Role(
                    roleId,
                    "Kế toán",
                    isActive:
                        false));

            await dbContext.SaveChangesAsync();
        }

        var persistence =
            new EfRoleActiveStatePersistence(
                new TestDbContextFactory(
                    options));

        RoleActiveStatePersistenceResult result =
            await persistence.TrySetAsync(
                roleId,
                false);

        Assert.Equal(
            RoleActiveStatePersistenceResult.Unchanged,
            result);
    }

    [Fact]
    public async Task
        TrySetAsync_WhenRoleDoesNotExist_ReturnsRoleNotFound()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await using (var dbContext =
                     new HrManagementDbContext(
                         options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();
        }

        var persistence =
            new EfRoleActiveStatePersistence(
                new TestDbContextFactory(
                    options));

        RoleActiveStatePersistenceResult result =
            await persistence.TrySetAsync(
                Guid.NewGuid(),
                false);

        Assert.Equal(
            RoleActiveStatePersistenceResult.RoleNotFound,
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
