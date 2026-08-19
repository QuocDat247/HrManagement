using HrManagement.Domain.Auditing;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Auditing;

public sealed class AuditEntryPersistenceTests
{
    [Fact]
    public async Task AuditEntry_CanBePersistedAndReadWithUtcTimestamp()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        Guid auditId =
            Guid.NewGuid();

        Guid entityId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        DateTime occurredAtUtc =
            new DateTime(
                2026,
                8,
                19,
                5,
                30,
                0,
                DateTimeKind.Utc);

        var entry =
            new AuditEntry(
                auditId,
                occurredAtUtc,
                "demo-admin",
                "admin",
                AuditAction.Updated,
                "EmployeePersonalProfile",
                entityId,
                employeeId);

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext.AuditEntries
                .AddAsync(
                    entry);

            await dbContext.SaveChangesAsync();
        }

        AuditEntry saved;

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            saved =
                await dbContext.AuditEntries
                    .AsNoTracking()
                    .SingleAsync(
                        item =>
                            item.Id ==
                            auditId);
        }

        Assert.Equal(
            auditId,
            saved.Id);

        Assert.Equal(
            occurredAtUtc,
            saved.OccurredAtUtc);

        Assert.Equal(
            DateTimeKind.Utc,
            saved.OccurredAtUtc.Kind);

        Assert.Equal(
            "demo-admin",
            saved.ActorUserId);

        Assert.Equal(
            "admin",
            saved.ActorUsername);

        Assert.Equal(
            AuditAction.Updated,
            saved.Action);

        Assert.Equal(
            "EmployeePersonalProfile",
            saved.EntityType);

        Assert.Equal(
            entityId,
            saved.EntityId);

        Assert.Equal(
            employeeId,
            saved.EmployeeId);
    }

    [Fact]
    public async Task EmployeeDeletion_DoesNotDeleteAuditEntry()
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

        Guid auditId =
            Guid.NewGuid();

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext.AuditEntries
                .AddAsync(
                    new AuditEntry(
                        auditId,
                        DateTime.UtcNow,
                        "demo-admin",
                        "admin",
                        AuditAction.Updated,
                        "EmployeeAddress",
                        Guid.NewGuid(),
                        employeeId));

            await dbContext.SaveChangesAsync();
        }

        await using (
            var dbContext =
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

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            AuditEntry? saved =
                await dbContext.AuditEntries
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id ==
                            auditId);

            Assert.NotNull(
                saved);

            Assert.Equal(
                employeeId,
                saved.EmployeeId);
        }
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
}
