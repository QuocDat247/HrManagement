using HrManagement.Application.Auditing;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;
using HrManagement.Infrastructure.Employees.Profiles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Auditing;

public sealed class AuditedEmployeeIdentificationRecordRepositoryTests
{
    [Fact]
    public async Task UpsertAsync_WhenCreatingRecord_WritesCreatedAudit()
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

        Guid recordId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        var repository =
            CreateRepository(
                options);

        await repository.UpsertAsync(
            new EmployeeIdentificationRecord(
                recordId,
                employeeId,
                EmployeeIdentificationType.NationalId,
                "012345678901"));

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
            AuditEntityTypes.EmployeeIdentificationRecord,
            audit.EntityType);

        Assert.Equal(
            recordId,
            audit.EntityId);

        Assert.Equal(
            employeeId,
            audit.EmployeeId);
    }

    [Fact]
    public async Task UpsertAsync_WhenUpdatingRecord_WritesUpdatedAudit()
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

        Guid recordId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        await AddRecordAsync(
            options,
            new EmployeeIdentificationRecord(
                recordId,
                employeeId,
                EmployeeIdentificationType.Other,
                "OLD-001"));

        var repository =
            CreateRepository(
                options);

        await repository.UpsertAsync(
            new EmployeeIdentificationRecord(
                recordId,
                employeeId,
                EmployeeIdentificationType.NationalId,
                "012345678901"));

        await using var dbContext =
            new HrManagementDbContext(
                options);

        AuditEntry audit =
            await dbContext.AuditEntries
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            AuditAction.Updated,
            audit.Action);

        Assert.Equal(
            recordId,
            audit.EntityId);

        Assert.Equal(
            employeeId,
            audit.EmployeeId);
    }

    [Fact]
    public async Task DeleteAsync_WhenRecordExists_WritesDeletedAudit()
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

        Guid recordId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        await AddRecordAsync(
            options,
            new EmployeeIdentificationRecord(
                recordId,
                employeeId,
                EmployeeIdentificationType.Passport,
                "P1234567"));

        var repository =
            CreateRepository(
                options);

        await repository.DeleteAsync(
            employeeId,
            recordId);

        await using var dbContext =
            new HrManagementDbContext(
                options);

        AuditEntry audit =
            await dbContext.AuditEntries
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            AuditAction.Deleted,
            audit.Action);

        Assert.Equal(
            AuditEntityTypes.EmployeeIdentificationRecord,
            audit.EntityType);

        Assert.Equal(
            recordId,
            audit.EntityId);

        Assert.Equal(
            employeeId,
            audit.EmployeeId);

        Assert.Empty(
            await dbContext
                .EmployeeIdentificationRecords
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task UpsertAsync_WhenAuditSaveFails_RollsBackRecordUpdate()
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

        Guid recordId =
            Guid.NewGuid();

        Guid duplicateAuditId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        await AddRecordAsync(
            options,
            new EmployeeIdentificationRecord(
                recordId,
                employeeId,
                EmployeeIdentificationType.Other,
                "OLD-SECRET"));

        await AddExistingAuditAsync(
            options,
            duplicateAuditId,
            employeeId,
            recordId);

        var repository =
            new EfEmployeeIdentificationRecordRepository(
                new TestDbContextFactory(
                    options),
                new FixedAuditEntryFactory(
                    duplicateAuditId));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                repository.UpsertAsync(
                    new EmployeeIdentificationRecord(
                        recordId,
                        employeeId,
                        EmployeeIdentificationType.NationalId,
                        "NEW-SECRET")));

        await using var dbContext =
            new HrManagementDbContext(
                options);

        EmployeeIdentificationRecord saved =
            await dbContext
                .EmployeeIdentificationRecords
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            EmployeeIdentificationType.Other,
            saved.Type);

        Assert.Equal(
            "OLD-SECRET",
            saved.DocumentNumber);
    }

    [Fact]
    public async Task DeleteAsync_WhenAuditSaveFails_RollsBackRecordDeletion()
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

        Guid recordId =
            Guid.NewGuid();

        Guid duplicateAuditId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        await AddRecordAsync(
            options,
            new EmployeeIdentificationRecord(
                recordId,
                employeeId,
                EmployeeIdentificationType.Passport,
                "KEEP-ME"));

        await AddExistingAuditAsync(
            options,
            duplicateAuditId,
            employeeId,
            recordId);

        var repository =
            new EfEmployeeIdentificationRecordRepository(
                new TestDbContextFactory(
                    options),
                new FixedAuditEntryFactory(
                    duplicateAuditId));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                repository.DeleteAsync(
                    employeeId,
                    recordId));

        await using var dbContext =
            new HrManagementDbContext(
                options);

        EmployeeIdentificationRecord saved =
            await dbContext
                .EmployeeIdentificationRecords
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            recordId,
            saved.Id);

        Assert.Equal(
            "KEEP-ME",
            saved.DocumentNumber);
    }

    private static EfEmployeeIdentificationRecordRepository
        CreateRepository(
            DbContextOptions<HrManagementDbContext> options)
    {
        return new EfEmployeeIdentificationRecordRepository(
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

        await dbContext.Employees
            .AddAsync(
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
                    EmployeeStatus.Active));

        await dbContext.SaveChangesAsync();
    }

    private static async Task AddRecordAsync(
        DbContextOptions<HrManagementDbContext> options,
        EmployeeIdentificationRecord record)
    {
        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext
            .EmployeeIdentificationRecords
            .AddAsync(
                record);

        await dbContext.SaveChangesAsync();
    }

    private static async Task AddExistingAuditAsync(
        DbContextOptions<HrManagementDbContext> options,
        Guid auditId,
        Guid employeeId,
        Guid entityId)
    {
        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.AuditEntries
            .AddAsync(
                new AuditEntry(
                    auditId,
                    DateTime.UtcNow,
                    "existing-user",
                    "existing",
                    AuditAction.Created,
                    AuditEntityTypes.EmployeeIdentificationRecord,
                    entityId,
                    employeeId));

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
