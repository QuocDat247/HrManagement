using HrManagement.Application.Auditing;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;
using HrManagement.Infrastructure.Employees.Profiles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Auditing;

public sealed class AuditedEmployeeEmergencyContactRepositoryTests
{
    [Fact]
    public async Task UpsertAsync_WhenCreatingContact_WritesCreatedAudit()
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

        Guid contactId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        var repository =
            CreateRepository(
                options);

        await repository.UpsertAsync(
            new EmployeeEmergencyContact(
                contactId,
                employeeId,
                "Nguyễn Văn A",
                "Cha",
                "0901000001"));

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
            AuditEntityTypes.EmployeeEmergencyContact,
            audit.EntityType);

        Assert.Equal(
            contactId,
            audit.EntityId);

        Assert.Equal(
            employeeId,
            audit.EmployeeId);
    }

    [Fact]
    public async Task UpsertAsync_WhenUpdatingContact_WritesUpdatedAudit()
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

        Guid contactId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        await AddContactAsync(
            options,
            new EmployeeEmergencyContact(
                contactId,
                employeeId,
                "Tên cũ",
                "Bạn",
                "0901000001"));

        var repository =
            CreateRepository(
                options);

        await repository.UpsertAsync(
            new EmployeeEmergencyContact(
                contactId,
                employeeId,
                "Tên mới",
                "Anh trai",
                "0902000002"));

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
            contactId,
            audit.EntityId);

        Assert.Equal(
            employeeId,
            audit.EmployeeId);
    }

    [Fact]
    public async Task UpsertAsync_WhenReplacingPrimary_AuditsBothMutations()
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

        Guid previousPrimaryId =
            Guid.NewGuid();

        Guid newPrimaryId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        await AddContactAsync(
            options,
            new EmployeeEmergencyContact(
                previousPrimaryId,
                employeeId,
                "Liên hệ chính cũ",
                "Cha",
                "0901000001",
                isPrimary: true));

        var repository =
            CreateRepository(
                options);

        await repository.UpsertAsync(
            new EmployeeEmergencyContact(
                newPrimaryId,
                employeeId,
                "Liên hệ chính mới",
                "Mẹ",
                "0902000002",
                isPrimary: true));

        await using var dbContext =
            new HrManagementDbContext(
                options);

        List<AuditEntry> audits =
            await dbContext.AuditEntries
                .AsNoTracking()
                .ToListAsync();

        Assert.Equal(
            2,
            audits.Count);

        AuditEntry newPrimaryAudit =
            Assert.Single(
                audits.Where(
                    audit =>
                        audit.EntityId ==
                        newPrimaryId));

        Assert.Equal(
            AuditAction.Created,
            newPrimaryAudit.Action);

        AuditEntry previousPrimaryAudit =
            Assert.Single(
                audits.Where(
                    audit =>
                        audit.EntityId ==
                        previousPrimaryId));

        Assert.Equal(
            AuditAction.Updated,
            previousPrimaryAudit.Action);

        List<EmployeeEmergencyContact> contacts =
            await dbContext
                .EmployeeEmergencyContacts
                .AsNoTracking()
                .ToListAsync();

        EmployeeEmergencyContact previousPrimary =
            Assert.Single(
                contacts.Where(
                    contact =>
                        contact.Id ==
                        previousPrimaryId));

        EmployeeEmergencyContact newPrimary =
            Assert.Single(
                contacts.Where(
                    contact =>
                        contact.Id ==
                        newPrimaryId));

        Assert.False(
            previousPrimary.IsPrimary);

        Assert.True(
            newPrimary.IsPrimary);
    }

    [Fact]
    public async Task DeleteAsync_WhenContactExists_WritesDeletedAudit()
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

        Guid contactId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        await AddContactAsync(
            options,
            new EmployeeEmergencyContact(
                contactId,
                employeeId,
                "Liên hệ cần xóa",
                "Bạn",
                "0901000001"));

        var repository =
            CreateRepository(
                options);

        await repository.DeleteAsync(
            employeeId,
            contactId);

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
            contactId,
            audit.EntityId);

        Assert.Equal(
            employeeId,
            audit.EmployeeId);

        Assert.Empty(
            await dbContext
                .EmployeeEmergencyContacts
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task UpsertAsync_WhenAuditSaveFails_RollsBackContactUpdate()
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

        Guid contactId =
            Guid.NewGuid();

        Guid duplicateAuditId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        await AddContactAsync(
            options,
            new EmployeeEmergencyContact(
                contactId,
                employeeId,
                "Giá trị cũ",
                "Bạn",
                "0901000001"));

        await AddExistingAuditAsync(
            options,
            duplicateAuditId,
            employeeId,
            contactId);

        var repository =
            new EfEmployeeEmergencyContactRepository(
                new TestDbContextFactory(
                    options),
                new FixedAuditEntryFactory(
                    duplicateAuditId));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                repository.UpsertAsync(
                    new EmployeeEmergencyContact(
                        contactId,
                        employeeId,
                        "Giá trị mới",
                        "Bạn",
                        "0902000002")));

        await using var dbContext =
            new HrManagementDbContext(
                options);

        EmployeeEmergencyContact saved =
            await dbContext
                .EmployeeEmergencyContacts
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            "Giá trị cũ",
            saved.FullName);

        Assert.Equal(
            "0901000001",
            saved.PhoneNumber);
    }

    [Fact]
    public async Task DeleteAsync_WhenAuditSaveFails_RollsBackContactDeletion()
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

        Guid contactId =
            Guid.NewGuid();

        Guid duplicateAuditId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        await AddContactAsync(
            options,
            new EmployeeEmergencyContact(
                contactId,
                employeeId,
                "Liên hệ cần giữ lại",
                "Bạn",
                "0901000001"));

        await AddExistingAuditAsync(
            options,
            duplicateAuditId,
            employeeId,
            contactId);

        var repository =
            new EfEmployeeEmergencyContactRepository(
                new TestDbContextFactory(
                    options),
                new FixedAuditEntryFactory(
                    duplicateAuditId));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                repository.DeleteAsync(
                    employeeId,
                    contactId));

        await using var dbContext =
            new HrManagementDbContext(
                options);

        EmployeeEmergencyContact saved =
            await dbContext
                .EmployeeEmergencyContacts
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            contactId,
            saved.Id);
    }

    private static EfEmployeeEmergencyContactRepository
        CreateRepository(
            DbContextOptions<HrManagementDbContext> options)
    {
        return new EfEmployeeEmergencyContactRepository(
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

    private static async Task AddContactAsync(
        DbContextOptions<HrManagementDbContext> options,
        EmployeeEmergencyContact contact)
    {
        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.EmployeeEmergencyContacts
            .AddAsync(
                contact);

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
                    AuditEntityTypes.EmployeeEmergencyContact,
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
