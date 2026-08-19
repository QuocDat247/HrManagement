using HrManagement.Application.Employees.Profiles;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;
using HrManagement.Infrastructure.Employees.Profiles;
using HrManagement.Infrastructure.Persistence;
using HrManagement.Application.Auditing;
using HrManagement.Domain.Auditing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Employees;

public sealed class EfEmployeeEmergencyContactRepositoryTests
{
    [Fact]
    public async Task GetByEmployeeIdAsync_WhenNoneExist_ReturnsEmpty()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(connection);

        await EnsureCreatedAsync(options);

        Guid employeeId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        IEmployeeEmergencyContactRepository repository =
            CreateRepository(options);

        IReadOnlyList<EmployeeEmergencyContact> contacts =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        Assert.Empty(contacts);
    }

    [Fact]
    public async Task UpsertAsync_WhenNew_AddsContact()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(connection);

        await EnsureCreatedAsync(options);

        Guid employeeId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        IEmployeeEmergencyContactRepository repository =
            CreateRepository(options);

        Guid contactId =
            Guid.NewGuid();

        var contact =
            new EmployeeEmergencyContact(
                contactId,
                employeeId,
                "Nguyễn Văn Bình",
                "Cha",
                "0901234567",
                "binh@example.com",
                isPrimary: true);

        await repository.UpsertAsync(
            contact);

        IReadOnlyList<EmployeeEmergencyContact> contacts =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        EmployeeEmergencyContact saved =
            Assert.Single(contacts);

        Assert.Equal(
            contactId,
            saved.Id);

        Assert.Equal(
            employeeId,
            saved.EmployeeId);

        Assert.Equal(
            "Nguyễn Văn Bình",
            saved.FullName);

        Assert.Equal(
            "Cha",
            saved.Relationship);

        Assert.Equal(
            "0901234567",
            saved.PhoneNumber);

        Assert.Equal(
            "binh@example.com",
            saved.Email);

        Assert.True(
            saved.IsPrimary);
    }

    [Fact]
    public async Task UpsertAsync_AllowsMultipleNonPrimaryContacts()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(connection);

        await EnsureCreatedAsync(options);

        Guid employeeId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        IEmployeeEmergencyContactRepository repository =
            CreateRepository(options);

        await repository.UpsertAsync(
            new EmployeeEmergencyContact(
                Guid.NewGuid(),
                employeeId,
                "Nguyễn Văn A",
                "Cha",
                "0901000001"));

        await repository.UpsertAsync(
            new EmployeeEmergencyContact(
                Guid.NewGuid(),
                employeeId,
                "Nguyễn Thị B",
                "Mẹ",
                "0901000002"));

        IReadOnlyList<EmployeeEmergencyContact> contacts =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        Assert.Equal(
            2,
            contacts.Count);

        Assert.All(
            contacts,
            contact =>
                Assert.False(
                    contact.IsPrimary));
    }

    [Fact]
    public async Task UpsertAsync_WhenNewPrimary_ReplacesExistingPrimary()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(connection);

        await EnsureCreatedAsync(options);

        Guid employeeId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        IEmployeeEmergencyContactRepository repository =
            CreateRepository(options);

        Guid firstId =
            Guid.NewGuid();

        await repository.UpsertAsync(
            new EmployeeEmergencyContact(
                firstId,
                employeeId,
                "Nguyễn Văn A",
                "Cha",
                "0901000001",
                isPrimary: true));

        Guid secondId =
            Guid.NewGuid();

        await repository.UpsertAsync(
            new EmployeeEmergencyContact(
                secondId,
                employeeId,
                "Nguyễn Thị B",
                "Mẹ",
                "0901000002",
                isPrimary: true));

        IReadOnlyList<EmployeeEmergencyContact> contacts =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        Assert.Equal(
            2,
            contacts.Count);

        EmployeeEmergencyContact primary =
            Assert.Single(
                contacts.Where(
                    contact =>
                        contact.IsPrimary));

        Assert.Equal(
            secondId,
            primary.Id);

        EmployeeEmergencyContact previousPrimary =
            Assert.Single(
                contacts.Where(
                    contact =>
                        contact.Id ==
                        firstId));

        Assert.False(
            previousPrimary.IsPrimary);
    }

    [Fact]
    public async Task UpsertAsync_WhenExisting_UpdatesAndPreservesId()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(connection);

        await EnsureCreatedAsync(options);

        Guid employeeId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        IEmployeeEmergencyContactRepository repository =
            CreateRepository(options);

        Guid contactId =
            Guid.NewGuid();

        await repository.UpsertAsync(
            new EmployeeEmergencyContact(
                contactId,
                employeeId,
                "Tên cũ",
                "Bạn",
                "0901111111"));

        await repository.UpsertAsync(
            new EmployeeEmergencyContact(
                contactId,
                employeeId,
                "Tên mới",
                "Anh trai",
                "0902222222",
                "new@example.com",
                isPrimary: true));

        IReadOnlyList<EmployeeEmergencyContact> contacts =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        EmployeeEmergencyContact updated =
            Assert.Single(contacts);

        Assert.Equal(
            contactId,
            updated.Id);

        Assert.Equal(
            employeeId,
            updated.EmployeeId);

        Assert.Equal(
            "Tên mới",
            updated.FullName);

        Assert.Equal(
            "Anh trai",
            updated.Relationship);

        Assert.Equal(
            "0902222222",
            updated.PhoneNumber);

        Assert.Equal(
            "new@example.com",
            updated.Email);

        Assert.True(
            updated.IsPrimary);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyRequestedContact()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(connection);

        await EnsureCreatedAsync(options);

        Guid employeeId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        IEmployeeEmergencyContactRepository repository =
            CreateRepository(options);

        Guid firstId =
            Guid.NewGuid();

        Guid secondId =
            Guid.NewGuid();

        await repository.UpsertAsync(
            new EmployeeEmergencyContact(
                firstId,
                employeeId,
                "Người thứ nhất",
                "Cha",
                "0901000001"));

        await repository.UpsertAsync(
            new EmployeeEmergencyContact(
                secondId,
                employeeId,
                "Người thứ hai",
                "Mẹ",
                "0901000002"));

        await repository.DeleteAsync(
            employeeId,
            firstId);

        IReadOnlyList<EmployeeEmergencyContact> contacts =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        EmployeeEmergencyContact remaining =
            Assert.Single(contacts);

        Assert.Equal(
            secondId,
            remaining.Id);
    }

    [Fact]
    public async Task EmployeeDeletion_CascadesEmergencyContacts()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(connection);

        await EnsureCreatedAsync(options);

        Guid employeeId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        IEmployeeEmergencyContactRepository repository =
            CreateRepository(options);

        await repository.UpsertAsync(
            new EmployeeEmergencyContact(
                Guid.NewGuid(),
                employeeId,
                "Nguyễn Văn A",
                "Cha",
                "0901000001",
                isPrimary: true));

        await repository.UpsertAsync(
            new EmployeeEmergencyContact(
                Guid.NewGuid(),
                employeeId,
                "Nguyễn Thị B",
                "Mẹ",
                "0901000002"));

        await using (var dbContext =
                     new HrManagementDbContext(
                         options))
        {
            Employee employee =
                await dbContext.Employees
                    .SingleAsync(
                        item =>
                            item.Id ==
                            employeeId);

            dbContext.Employees.Remove(
                employee);

            await dbContext.SaveChangesAsync();
        }

        IReadOnlyList<EmployeeEmergencyContact> contacts =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        Assert.Empty(contacts);
    }

    [Fact]
    public async Task UpsertAsync_WhenPrimaryWriteFails_RollsBackPreviousPrimary()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(connection);

        await EnsureCreatedAsync(options);

        Guid employeeId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            employeeId);

        IEmployeeEmergencyContactRepository repository =
            CreateRepository(options);

        Guid existingPrimaryId =
            Guid.NewGuid();

        await repository.UpsertAsync(
            new EmployeeEmergencyContact(
                existingPrimaryId,
                employeeId,
                "Liên hệ chính hiện tại",
                "Cha",
                "0901000001",
                isPrimary: true));

        await using (var dbContext =
                     new HrManagementDbContext(
                         options))
        {
            await dbContext.Database
                .ExecuteSqlRawAsync(
                    """
                    CREATE TRIGGER TR_EmployeeEmergencyContacts_TestFailure
                    BEFORE INSERT ON EmployeeEmergencyContacts
                    WHEN NEW.FullName = 'FORCE_FAILURE'
                    BEGIN
                        SELECT RAISE(ABORT, 'Forced test failure');
                    END;
                    """);
        }

        var failingContact =
            new EmployeeEmergencyContact(
                Guid.NewGuid(),
                employeeId,
                "FORCE_FAILURE",
                "Mẹ",
                "0902000002",
                isPrimary: true);

        await Assert.ThrowsAnyAsync<Exception>(
            () =>
                repository.UpsertAsync(
                    failingContact));

        IReadOnlyList<EmployeeEmergencyContact> contacts =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        EmployeeEmergencyContact primary =
            Assert.Single(
                contacts.Where(
                    contact =>
                        contact.IsPrimary));

        Assert.Equal(
            existingPrimaryId,
            primary.Id);

        Assert.DoesNotContain(
            contacts,
            contact =>
                contact.FullName ==
                "FORCE_FAILURE");
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

    private static IEmployeeEmergencyContactRepository
        CreateRepository(
            DbContextOptions<HrManagementDbContext> options)
    {
        return new EfEmployeeEmergencyContactRepository(
            new TestDbContextFactory(
                options),
            new TestAuditEntryFactory());
    }

    private static async Task AddEmployeeAsync(
        DbContextOptions<HrManagementDbContext> options,
        Guid employeeId)
    {
        await using var dbContext =
            new HrManagementDbContext(
                options);

        string employeeCode =
            $"EMP-{employeeId:N}";

        if (employeeCode.Length > 20)
        {
            employeeCode =
                employeeCode[..20];
        }

        var employee =
            new Employee(
                employeeId,
                employeeCode,
                "Nhân viên kiểm thử",
                null,
                null,
                null,
                new DateOnly(
                    2025,
                    1,
                    1),
                "Phòng kiểm thử",
                "Chuyên viên",
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

        public HrManagementDbContext
            CreateDbContext()
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
}
