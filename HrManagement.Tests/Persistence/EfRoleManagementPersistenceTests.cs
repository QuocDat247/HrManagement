using HrManagement.Application.Authorization.Roles;
using HrManagement.Domain.Authorization.Roles;
using HrManagement.Infrastructure.Authorization.Roles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Persistence;

public sealed class EfRoleManagementPersistenceTests
{
    [Fact]
    public async Task
        TryCreateAsync_PersistsRole()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureDatabaseAsync(
            options);

        var persistence =
            new EfRoleManagementPersistence(
                new TestDbContextFactory(
                    options));

        var role =
            new Role(
                Guid.NewGuid(),
                "Quản lý nhân sự",
                "Quản lý hồ sơ.");

        RoleManagementPersistenceResult result =
            await persistence.TryCreateAsync(
                role);

        Assert.Equal(
            RoleManagementPersistenceResult.Created,
            result);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        Role saved =
            await verificationContext
                .Roles
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            role.Id,
            saved.Id);

        Assert.Equal(
            "Quản lý nhân sự",
            saved.Name);
    }

    [Fact]
    public async Task
        TryCreateAsync_WithDuplicateName_DoesNotPersistSecondRole()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureDatabaseAsync(
            options);

        var persistence =
            new EfRoleManagementPersistence(
                new TestDbContextFactory(
                    options));

        await persistence.TryCreateAsync(
            new Role(
                Guid.NewGuid(),
                "Quản lý"));

        RoleManagementPersistenceResult result =
            await persistence.TryCreateAsync(
                new Role(
                    Guid.NewGuid(),
                    "  QUẢN LÝ  "));

        Assert.Equal(
            RoleManagementPersistenceResult
                .NameAlreadyExists,
            result);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        Assert.Equal(
            1,
            await verificationContext
                .Roles
                .CountAsync());
    }

    [Fact]
    public async Task
        TryUpdateAsync_ChangesDetailsAndPreservesActiveState()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureDatabaseAsync(
            options);

        Guid roleId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(
                         options))
        {
            dbContext.Roles.Add(
                new Role(
                    roleId,
                    "Tên cũ",
                    isActive:
                        false));

            await dbContext.SaveChangesAsync();
        }

        var persistence =
            new EfRoleManagementPersistence(
                new TestDbContextFactory(
                    options));

        RoleManagementPersistenceResult result =
            await persistence.TryUpdateAsync(
                roleId,
                "Tên mới",
                "Mô tả mới");

        Assert.Equal(
            RoleManagementPersistenceResult.Updated,
            result);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        Role saved =
            await verificationContext
                .Roles
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            "Tên mới",
            saved.Name);

        Assert.Equal(
            "TÊN MỚI",
            saved.NormalizedName);

        Assert.Equal(
            "Mô tả mới",
            saved.Description);

        Assert.False(
            saved.IsActive);
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

    private static async Task EnsureDatabaseAsync(
        DbContextOptions<HrManagementDbContext> options)
    {
        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.Database
            .EnsureCreatedAsync();
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
