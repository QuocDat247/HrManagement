using HrManagement.Domain.Leave.Types;
using HrManagement.Infrastructure.Leave.Types;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Leave;

public sealed class LeaveTypeSeedServiceTests
{
    [Fact]
    public async Task EmptyDatabase_SeedsDefaultLeaveTypes()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        var service =
            CreateService(
                options);

        await service.SeedAsync();

        await using var verification =
            new HrManagementDbContext(
                options);

        List<LeaveType> types =
            await verification
                .LeaveTypes
                .AsNoTracking()
                .OrderBy(
                    type =>
                        type.Code)
                .ToListAsync();

        Assert.Equal(
            3,
            types.Count);

        LeaveType annual =
            Assert.Single(
                types.Where(
                    type =>
                        type.Code ==
                        "ANNUAL"));

        Assert.Equal(
            LeaveTypeSeedService.AnnualLeaveTypeId,
            annual.Id);

        Assert.Equal(
            "Nghỉ phép năm",
            annual.Name);

        Assert.True(
            annual.IsPaid);

        Assert.True(
            annual.IsActive);

        LeaveType sick =
            Assert.Single(
                types.Where(
                    type =>
                        type.Code ==
                        "SICK"));

        Assert.Equal(
            LeaveTypeSeedService.SickLeaveTypeId,
            sick.Id);

        Assert.True(
            sick.IsPaid);

        LeaveType unpaid =
            Assert.Single(
                types.Where(
                    type =>
                        type.Code ==
                        "UNPAID"));

        Assert.Equal(
            LeaveTypeSeedService.UnpaidLeaveTypeId,
            unpaid.Id);

        Assert.False(
            unpaid.IsPaid);
    }

    [Fact]
    public async Task SeedTwice_IsIdempotent()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        var service =
            CreateService(
                options);

        await service.SeedAsync();
        await service.SeedAsync();

        await using var verification =
            new HrManagementDbContext(
                options);

        Assert.Equal(
            3,
            await verification
                .LeaveTypes
                .CountAsync());
    }

    [Fact]
    public async Task ExistingDefaultCode_IsPreservedAndMissingTypesAreAdded()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        Guid existingAnnualId =
            Guid.NewGuid();

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext
                .LeaveTypes
                .AddAsync(
                    new LeaveType(
                        existingAnnualId,
                        "ANNUAL",
                        "Phép năm tùy chỉnh",
                        isPaid: false,
                        isActive: false));

            await dbContext.SaveChangesAsync();
        }

        var service =
            CreateService(
                options);

        await service.SeedAsync();

        await using var verification =
            new HrManagementDbContext(
                options);

        List<LeaveType> types =
            await verification
                .LeaveTypes
                .AsNoTracking()
                .ToListAsync();

        Assert.Equal(
            3,
            types.Count);

        LeaveType annual =
            Assert.Single(
                types.Where(
                    type =>
                        type.Code ==
                        "ANNUAL"));

        Assert.Equal(
            existingAnnualId,
            annual.Id);

        Assert.Equal(
            "Phép năm tùy chỉnh",
            annual.Name);

        Assert.False(
            annual.IsPaid);

        Assert.False(
            annual.IsActive);

        Assert.Contains(
            types,
            type =>
                type.Code ==
                "SICK");

        Assert.Contains(
            types,
            type =>
                type.Code ==
                "UNPAID");
    }

    [Fact]
    public async Task UnrelatedCustomType_IsPreserved()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        Guid customId =
            Guid.NewGuid();

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext
                .LeaveTypes
                .AddAsync(
                    new LeaveType(
                        customId,
                        "BEREAVEMENT",
                        "Nghỉ tang",
                        isPaid: true));

            await dbContext.SaveChangesAsync();
        }

        var service =
            CreateService(
                options);

        await service.SeedAsync();

        await using var verification =
            new HrManagementDbContext(
                options);

        Assert.Equal(
            4,
            await verification
                .LeaveTypes
                .CountAsync());

        LeaveType custom =
            await verification
                .LeaveTypes
                .AsNoTracking()
                .SingleAsync(
                    type =>
                        type.Id ==
                        customId);

        Assert.Equal(
            "BEREAVEMENT",
            custom.Code);
    }

    [Fact]
    public async Task ReservedDefaultIdUsedByDifferentCode_Throws()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext
                .LeaveTypes
                .AddAsync(
                    new LeaveType(
                        LeaveTypeSeedService.AnnualLeaveTypeId,
                        "CUSTOM",
                        "Loại nghỉ tùy chỉnh",
                        isPaid: true));

            await dbContext.SaveChangesAsync();
        }

        var service =
            CreateService(
                options);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                service.SeedAsync());

        await using var verification =
            new HrManagementDbContext(
                options);

        Assert.Single(
            await verification
                .LeaveTypes
                .ToListAsync());
    }

    private static LeaveTypeSeedService CreateService(
        DbContextOptions<HrManagementDbContext> options)
    {
        return new LeaveTypeSeedService(
            new TestDbContextFactory(
                options));
    }

    private static async Task<SqliteConnection>
        CreateOpenConnectionAsync()
    {
        var connection =
            new SqliteConnection(
                "Data Source=:memory:;Foreign Keys=True");

        await connection.OpenAsync();

        return connection;
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

    private static async Task EnsureCreatedAsync(
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
                CreateDbContext());
        }
    }
}
