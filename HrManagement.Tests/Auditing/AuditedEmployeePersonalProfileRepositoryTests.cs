using HrManagement.Application.Auditing;
using HrManagement.Application.Employees.Profiles;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;
using HrManagement.Infrastructure.Employees.Profiles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Auditing;

public sealed class AuditedEmployeePersonalProfileRepositoryTests
{
    [Fact]
    public async Task UpsertAsync_WhenCreatingProfile_WritesCreatedAudit()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        Guid employeeId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        var repository =
            CreateRepository(
                options);

        await repository.UpsertAsync(
            new EmployeePersonalProfile(
                employeeId,
                "An",
                EmployeeGender.Male,
                "Việt Nam",
                "Hà Nội"));

        await using var dbContext =
            new HrManagementDbContext(
                options);

        AuditEntry audit =
            await dbContext.AuditEntries
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            AuditAction.Created,
            audit.Action);

        Assert.Equal(
            AuditEntityTypes.EmployeePersonalProfile,
            audit.EntityType);

        Assert.Equal(
            employeeId,
            audit.EntityId);

        Assert.Equal(
            employeeId,
            audit.EmployeeId);
    }

    [Fact]
    public async Task UpsertAsync_WhenUpdatingProfile_WritesUpdatedAudit()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        Guid employeeId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext.EmployeePersonalProfiles
                .AddAsync(
                    new EmployeePersonalProfile(
                        employeeId,
                        "An"));

            await dbContext.SaveChangesAsync();
        }

        var repository =
            CreateRepository(
                options);

        await repository.UpsertAsync(
            new EmployeePersonalProfile(
                employeeId,
                "An Nguyễn",
                EmployeeGender.Male,
                "Việt Nam",
                "Đà Nẵng"));

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        AuditEntry audit =
            await verificationContext.AuditEntries
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            AuditAction.Updated,
            audit.Action);

        Assert.Equal(
            employeeId,
            audit.EntityId);

        Assert.Equal(
            employeeId,
            audit.EmployeeId);
    }

    [Fact]
    public async Task UpsertAsync_WhenAuditSaveFails_RollsBackProfileChange()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        Guid employeeId =
            Guid.NewGuid();

        Guid duplicateAuditId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext.EmployeePersonalProfiles
                .AddAsync(
                    new EmployeePersonalProfile(
                        employeeId,
                        "Giá trị cũ"));

            await dbContext.AuditEntries
                .AddAsync(
                    new AuditEntry(
                        duplicateAuditId,
                        DateTime.UtcNow,
                        "existing-user",
                        "existing",
                        AuditAction.Created,
                        AuditEntityTypes.EmployeePersonalProfile,
                        employeeId,
                        employeeId));

            await dbContext.SaveChangesAsync();
        }

        var repository =
            new EfEmployeePersonalProfileRepository(
                new TestDbContextFactory(
                    options),
                new FixedAuditEntryFactory(
                    duplicateAuditId));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                repository.UpsertAsync(
                    new EmployeePersonalProfile(
                        employeeId,
                        "Giá trị mới")));

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        EmployeePersonalProfile saved =
            await verificationContext
                .EmployeePersonalProfiles
                .AsNoTracking()
                .SingleAsync(
                    profile =>
                        profile.EmployeeId ==
                        employeeId);

        Assert.Equal(
            "Giá trị cũ",
            saved.PreferredName);
    }

    private static EfEmployeePersonalProfileRepository
        CreateRepository(
            DbContextOptions<HrManagementDbContext> options)
    {
        return new EfEmployeePersonalProfileRepository(
            new TestDbContextFactory(
                options),
            new TestAuditEntryFactory());
    }

    private static async Task<SqliteConnection>
        CreateOpenConnectionAsync()
    {
        var connection =
            new SqliteConnection(
                "Data Source=:memory:");

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

    private static async Task AddEmployeeAsync(
        DbContextOptions<HrManagementDbContext> options,
        Guid employeeId)
    {
        await using var dbContext =
            new HrManagementDbContext(
                options);

        var employee =
            new Employee(
                employeeId,
                $"EMP-{employeeId:N}"[..20],
                "Nhân viên kiểm thử",
                null,
                null,
                null,
                new DateOnly(
                    2025,
                    1,
                    1),
                "Phòng kiểm thử",
                "Chuyên viên kiểm thử",
                EmployeeStatus.Active);

        await dbContext.Employees
            .AddAsync(
                employee);

        await dbContext.SaveChangesAsync();
    }

    private sealed class TestDbContextFactory
        : IDbContextFactory<HrManagementDbContext>
    {
        private readonly
            DbContextOptions<HrManagementDbContext>
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

    private sealed class TestAuditEntryFactory
        : IAuditEntryFactory
    {
        public AuditEntry Create(
            AuditAction action,
            string entityType,
            Guid entityId,
            Guid? employeeId = null)
        {
            return new AuditEntry(
                Guid.NewGuid(),
                DateTime.UtcNow,
                "test-user",
                "test",
                action,
                entityType,
                entityId,
                employeeId);
        }
    }

    private sealed class FixedAuditEntryFactory
        : IAuditEntryFactory
    {
        private readonly Guid
            _auditId;

        public FixedAuditEntryFactory(
            Guid auditId)
        {
            _auditId =
                auditId;
        }

        public AuditEntry Create(
            AuditAction action,
            string entityType,
            Guid entityId,
            Guid? employeeId = null)
        {
            return new AuditEntry(
                _auditId,
                DateTime.UtcNow,
                "test-user",
                "test",
                action,
                entityType,
                entityId,
                employeeId);
        }
    }
}
