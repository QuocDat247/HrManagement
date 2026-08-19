using HrManagement.Application.Auditing;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;
using HrManagement.Infrastructure.Employees.Profiles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Auditing;

public sealed class AuditedEmployeeAddressRepositoryTests
{
    [Fact]
    public async Task UpsertAsync_WhenCreatingAddress_WritesCreatedAudit()
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

        Guid addressId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        var repository =
            CreateRepository(
                options);

        await repository.UpsertAsync(
            new EmployeeAddress(
                addressId,
                employeeId,
                EmployeeAddressType.Permanent,
                "Địa chỉ thường trú"));

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
            AuditEntityTypes.EmployeeAddress,
            audit.EntityType);

        Assert.Equal(
            addressId,
            audit.EntityId);

        Assert.Equal(
            employeeId,
            audit.EmployeeId);
    }

    [Fact]
    public async Task UpsertAsync_WhenUpdatingAddress_AuditsPersistedAddressId()
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

        Guid originalAddressId =
            Guid.NewGuid();

        Guid incomingAddressId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        await AddAddressAsync(
            options,
            new EmployeeAddress(
                originalAddressId,
                employeeId,
                EmployeeAddressType.Current,
                "Địa chỉ cũ"));

        var repository =
            CreateRepository(
                options);

        await repository.UpsertAsync(
            new EmployeeAddress(
                incomingAddressId,
                employeeId,
                EmployeeAddressType.Current,
                "Địa chỉ mới"));

        await using var dbContext =
            new HrManagementDbContext(
                options);

        AuditEntry audit =
            await dbContext.AuditEntries
                .AsNoTracking()
                .SingleAsync();

        EmployeeAddress saved =
            await dbContext.EmployeeAddresses
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            AuditAction.Updated,
            audit.Action);

        Assert.Equal(
            originalAddressId,
            audit.EntityId);

        Assert.NotEqual(
            incomingAddressId,
            audit.EntityId);

        Assert.Equal(
            originalAddressId,
            saved.Id);

        Assert.Equal(
            "Địa chỉ mới",
            saved.AddressLine);
    }

    [Fact]
    public async Task DeleteAsync_WhenAddressExists_WritesDeletedAudit()
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

        Guid addressId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        await AddAddressAsync(
            options,
            new EmployeeAddress(
                addressId,
                employeeId,
                EmployeeAddressType.Current,
                "Địa chỉ hiện tại"));

        var repository =
            CreateRepository(
                options);

        await repository.DeleteAsync(
            employeeId,
            EmployeeAddressType.Current);

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
            AuditEntityTypes.EmployeeAddress,
            audit.EntityType);

        Assert.Equal(
            addressId,
            audit.EntityId);

        Assert.Equal(
            employeeId,
            audit.EmployeeId);

        Assert.Empty(
            await dbContext.EmployeeAddresses
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task UpsertAsync_WhenAuditSaveFails_RollsBackAddressUpdate()
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

        Guid addressId =
            Guid.NewGuid();

        Guid duplicateAuditId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        await AddAddressAsync(
            options,
            new EmployeeAddress(
                addressId,
                employeeId,
                EmployeeAddressType.Current,
                "Giá trị cũ"));

        await AddExistingAuditAsync(
            options,
            duplicateAuditId,
            employeeId,
            addressId);

        var repository =
            new EfEmployeeAddressRepository(
                new TestDbContextFactory(
                    options),
                new FixedAuditEntryFactory(
                    duplicateAuditId));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                repository.UpsertAsync(
                    new EmployeeAddress(
                        Guid.NewGuid(),
                        employeeId,
                        EmployeeAddressType.Current,
                        "Giá trị mới")));

        await using var dbContext =
            new HrManagementDbContext(
                options);

        EmployeeAddress saved =
            await dbContext.EmployeeAddresses
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            addressId,
            saved.Id);

        Assert.Equal(
            "Giá trị cũ",
            saved.AddressLine);
    }

    [Fact]
    public async Task DeleteAsync_WhenAuditSaveFails_RollsBackAddressDeletion()
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

        Guid addressId =
            Guid.NewGuid();

        Guid duplicateAuditId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        await AddAddressAsync(
            options,
            new EmployeeAddress(
                addressId,
                employeeId,
                EmployeeAddressType.Current,
                "Địa chỉ cần giữ lại"));

        await AddExistingAuditAsync(
            options,
            duplicateAuditId,
            employeeId,
            addressId);

        var repository =
            new EfEmployeeAddressRepository(
                new TestDbContextFactory(
                    options),
                new FixedAuditEntryFactory(
                    duplicateAuditId));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                repository.DeleteAsync(
                    employeeId,
                    EmployeeAddressType.Current));

        await using var dbContext =
            new HrManagementDbContext(
                options);

        EmployeeAddress saved =
            await dbContext.EmployeeAddresses
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            addressId,
            saved.Id);

        Assert.Equal(
            "Địa chỉ cần giữ lại",
            saved.AddressLine);
    }

    private static EfEmployeeAddressRepository
        CreateRepository(
            DbContextOptions<HrManagementDbContext> options)
    {
        return new EfEmployeeAddressRepository(
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

        await dbContext.Employees.AddAsync(
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

    private static async Task AddAddressAsync(
        DbContextOptions<HrManagementDbContext> options,
        EmployeeAddress address)
    {
        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.EmployeeAddresses
            .AddAsync(
                address);

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
                    AuditEntityTypes.EmployeeAddress,
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
