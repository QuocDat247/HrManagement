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

public sealed class EfEmployeeIdentificationRecordRepositoryTests
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

        IEmployeeIdentificationRecordRepository repository =
            CreateRepository(options);

        IReadOnlyList<EmployeeIdentificationRecord> records =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        Assert.Empty(records);
    }

    [Fact]
    public async Task UpsertAsync_WhenNew_AddsRecord()
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

        IEmployeeIdentificationRecordRepository repository =
            CreateRepository(options);

        Guid recordId =
            Guid.NewGuid();

        var record =
            new EmployeeIdentificationRecord(
                recordId,
                employeeId,
                EmployeeIdentificationType.Passport,
                "P1234567",
                issueDate:
                    new DateOnly(
                        2025,
                        1,
                        10),
                expiryDate:
                    new DateOnly(
                        2035,
                        1,
                        10),
                issuingAuthority:
                    "Cục Quản lý xuất nhập cảnh",
                placeOfIssue:
                    "Hà Nội",
                issuingCountry:
                    "Việt Nam");

        await repository.UpsertAsync(
            record);

        IReadOnlyList<EmployeeIdentificationRecord> records =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        EmployeeIdentificationRecord saved =
            Assert.Single(records);

        Assert.Equal(
            recordId,
            saved.Id);

        Assert.Equal(
            employeeId,
            saved.EmployeeId);

        Assert.Equal(
            EmployeeIdentificationType.Passport,
            saved.Type);

        Assert.Equal(
            "P1234567",
            saved.DocumentNumber);

        Assert.Equal(
            new DateOnly(
                2025,
                1,
                10),
            saved.IssueDate);

        Assert.Equal(
            new DateOnly(
                2035,
                1,
                10),
            saved.ExpiryDate);

        Assert.Equal(
            "Cục Quản lý xuất nhập cảnh",
            saved.IssuingAuthority);

        Assert.Equal(
            "Hà Nội",
            saved.PlaceOfIssue);

        Assert.Equal(
            "Việt Nam",
            saved.IssuingCountry);
    }

    [Fact]
    public async Task UpsertAsync_AllowsMultipleRecordsOfSameType()
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

        IEmployeeIdentificationRecordRepository repository =
            CreateRepository(options);

        await repository.UpsertAsync(
            new EmployeeIdentificationRecord(
                Guid.NewGuid(),
                employeeId,
                EmployeeIdentificationType.Passport,
                "P-OLD",
                issueDate:
                    new DateOnly(
                        2015,
                        1,
                        1),
                expiryDate:
                    new DateOnly(
                        2025,
                        1,
                        1)));

        await repository.UpsertAsync(
            new EmployeeIdentificationRecord(
                Guid.NewGuid(),
                employeeId,
                EmployeeIdentificationType.Passport,
                "P-NEW",
                issueDate:
                    new DateOnly(
                        2025,
                        2,
                        1),
                expiryDate:
                    new DateOnly(
                        2035,
                        2,
                        1)));

        IReadOnlyList<EmployeeIdentificationRecord> records =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        Assert.Equal(
            2,
            records.Count);

        Assert.All(
            records,
            record =>
                Assert.Equal(
                    EmployeeIdentificationType.Passport,
                    record.Type));

        Assert.Contains(
            records,
            record =>
                record.DocumentNumber ==
                "P-OLD");

        Assert.Contains(
            records,
            record =>
                record.DocumentNumber ==
                "P-NEW");
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

        IEmployeeIdentificationRecordRepository repository =
            CreateRepository(options);

        Guid recordId =
            Guid.NewGuid();

        await repository.UpsertAsync(
            new EmployeeIdentificationRecord(
                recordId,
                employeeId,
                EmployeeIdentificationType.Other,
                "OLD-001",
                issuingAuthority:
                    "Cơ quan cũ"));

        await repository.UpsertAsync(
            new EmployeeIdentificationRecord(
                recordId,
                employeeId,
                EmployeeIdentificationType.NationalId,
                "012345678901",
                issueDate:
                    new DateOnly(
                        2024,
                        6,
                        1),
                expiryDate:
                    new DateOnly(
                        2034,
                        6,
                        1),
                issuingAuthority:
                    "Cục Cảnh sát quản lý hành chính",
                placeOfIssue:
                    "TP. Hồ Chí Minh",
                issuingCountry:
                    "Việt Nam"));

        IReadOnlyList<EmployeeIdentificationRecord> records =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        EmployeeIdentificationRecord updated =
            Assert.Single(records);

        Assert.Equal(
            recordId,
            updated.Id);

        Assert.Equal(
            employeeId,
            updated.EmployeeId);

        Assert.Equal(
            EmployeeIdentificationType.NationalId,
            updated.Type);

        Assert.Equal(
            "012345678901",
            updated.DocumentNumber);

        Assert.Equal(
            new DateOnly(
                2024,
                6,
                1),
            updated.IssueDate);

        Assert.Equal(
            new DateOnly(
                2034,
                6,
                1),
            updated.ExpiryDate);

        Assert.Equal(
            "Cục Cảnh sát quản lý hành chính",
            updated.IssuingAuthority);

        Assert.Equal(
            "TP. Hồ Chí Minh",
            updated.PlaceOfIssue);

        Assert.Equal(
            "Việt Nam",
            updated.IssuingCountry);
    }

    [Fact]
    public async Task UpsertAsync_WhenRecordBelongsToAnotherEmployee_Throws()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(connection);

        await EnsureCreatedAsync(options);

        Guid firstEmployeeId =
            Guid.NewGuid();

        Guid secondEmployeeId =
            Guid.NewGuid();

        await AddEmployeeAsync(
            options,
            firstEmployeeId);

        await AddEmployeeAsync(
            options,
            secondEmployeeId);

        IEmployeeIdentificationRecordRepository repository =
            CreateRepository(options);

        Guid recordId =
            Guid.NewGuid();

        await repository.UpsertAsync(
            new EmployeeIdentificationRecord(
                recordId,
                firstEmployeeId,
                EmployeeIdentificationType.NationalId,
                "FIRST-001"));

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    repository.UpsertAsync(
                        new EmployeeIdentificationRecord(
                            recordId,
                            secondEmployeeId,
                            EmployeeIdentificationType.NationalId,
                            "SECOND-001")));

        Assert.Equal(
            "Không thể chuyển giấy tờ định danh sang nhân viên khác.",
            exception.Message);

        IReadOnlyList<EmployeeIdentificationRecord> firstEmployeeRecords =
            await repository.GetByEmployeeIdAsync(
                firstEmployeeId);

        EmployeeIdentificationRecord existing =
            Assert.Single(
                firstEmployeeRecords);

        Assert.Equal(
            "FIRST-001",
            existing.DocumentNumber);

        IReadOnlyList<EmployeeIdentificationRecord> secondEmployeeRecords =
            await repository.GetByEmployeeIdAsync(
                secondEmployeeId);

        Assert.Empty(
            secondEmployeeRecords);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyRequestedRecord()
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

        IEmployeeIdentificationRecordRepository repository =
            CreateRepository(options);

        Guid firstId =
            Guid.NewGuid();

        Guid secondId =
            Guid.NewGuid();

        await repository.UpsertAsync(
            new EmployeeIdentificationRecord(
                firstId,
                employeeId,
                EmployeeIdentificationType.NationalId,
                "012345678901"));

        await repository.UpsertAsync(
            new EmployeeIdentificationRecord(
                secondId,
                employeeId,
                EmployeeIdentificationType.Passport,
                "P1234567"));

        await repository.DeleteAsync(
            employeeId,
            firstId);

        IReadOnlyList<EmployeeIdentificationRecord> records =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        EmployeeIdentificationRecord remaining =
            Assert.Single(records);

        Assert.Equal(
            secondId,
            remaining.Id);

        Assert.Equal(
            "P1234567",
            remaining.DocumentNumber);
    }

    [Fact]
    public async Task EmployeeDeletion_CascadesIdentificationRecords()
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

        IEmployeeIdentificationRecordRepository repository =
            CreateRepository(options);

        await repository.UpsertAsync(
            new EmployeeIdentificationRecord(
                Guid.NewGuid(),
                employeeId,
                EmployeeIdentificationType.NationalId,
                "012345678901"));

        await repository.UpsertAsync(
            new EmployeeIdentificationRecord(
                Guid.NewGuid(),
                employeeId,
                EmployeeIdentificationType.Passport,
                "P1234567"));

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

        IReadOnlyList<EmployeeIdentificationRecord> records =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        Assert.Empty(records);
    }

    [Fact]
    public async Task GetByEmployeeIdAsync_OrdersByTypeThenIssueDateDescending()
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

        IEmployeeIdentificationRecordRepository repository =
            CreateRepository(options);

        await repository.UpsertAsync(
            new EmployeeIdentificationRecord(
                Guid.NewGuid(),
                employeeId,
                EmployeeIdentificationType.Passport,
                "PASSPORT-OLD",
                issueDate:
                    new DateOnly(
                        2018,
                        1,
                        1)));

        await repository.UpsertAsync(
            new EmployeeIdentificationRecord(
                Guid.NewGuid(),
                employeeId,
                EmployeeIdentificationType.NationalId,
                "NATIONAL-NEW",
                issueDate:
                    new DateOnly(
                        2024,
                        1,
                        1)));

        await repository.UpsertAsync(
            new EmployeeIdentificationRecord(
                Guid.NewGuid(),
                employeeId,
                EmployeeIdentificationType.Passport,
                "PASSPORT-NEW",
                issueDate:
                    new DateOnly(
                        2025,
                        1,
                        1)));

        await repository.UpsertAsync(
            new EmployeeIdentificationRecord(
                Guid.NewGuid(),
                employeeId,
                EmployeeIdentificationType.NationalId,
                "NATIONAL-OLD",
                issueDate:
                    new DateOnly(
                        2020,
                        1,
                        1)));

        IReadOnlyList<EmployeeIdentificationRecord> records =
            await repository.GetByEmployeeIdAsync(
                employeeId);

        Assert.Equal(
            4,
            records.Count);

        Assert.Equal(
            "NATIONAL-NEW",
            records[0].DocumentNumber);

        Assert.Equal(
            "NATIONAL-OLD",
            records[1].DocumentNumber);

        Assert.Equal(
            "PASSPORT-NEW",
            records[2].DocumentNumber);

        Assert.Equal(
            "PASSPORT-OLD",
            records[3].DocumentNumber);
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

    private static IEmployeeIdentificationRecordRepository
        CreateRepository(
            DbContextOptions<HrManagementDbContext> options)
    {
        return new EfEmployeeIdentificationRecordRepository(
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
