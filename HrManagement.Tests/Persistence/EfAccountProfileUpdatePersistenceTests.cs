using HrManagement.Application.Authentication.Accounts;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Authentication.Accounts;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Persistence;

public sealed class EfAccountProfileUpdatePersistenceTests
{
    [Fact]
    public async Task
        TryUpdateAsync_UpdatesProfileWithoutChangingSecurityIdentity()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<
                    HrManagementDbContext>()
                .UseSqlite(
                    connection)
                .Options;

        Guid accountId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(
                         options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Employees.Add(
                CreateEmployee(
                    employeeId,
                    "EMP001",
                    "Nguyễn Văn An"));

            dbContext.UserAccounts.Add(
                new UserAccount(
                    accountId,
                    "manager",
                    "Tên cũ",
                    UserAccountKind.Standard,
                    isActive:
                        false));

            await dbContext.SaveChangesAsync();
        }

        var persistence =
            new EfAccountProfileUpdatePersistence(
                new TestDbContextFactory(
                    options));

        AccountProfileUpdatePersistenceResult result =
            await persistence.TryUpdateAsync(
                accountId,
                "  Quản lý mới  ",
                employeeId);

        Assert.Equal(
            AccountProfileUpdatePersistenceResult.Updated,
            result);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        UserAccount saved =
            await verificationContext
                .UserAccounts
                .AsNoTracking()
                .SingleAsync(
                    account =>
                        account.Id ==
                        accountId);

        Assert.Equal(
            "Quản lý mới",
            saved.DisplayName);

        Assert.Equal(
            employeeId,
            saved.EmployeeId);

        Assert.Equal(
            "manager",
            saved.Username);

        Assert.Equal(
            UserAccountKind.Standard,
            saved.Kind);

        Assert.False(
            saved.IsActive);
    }

    [Fact]
    public async Task
        TryUpdateAsync_WhenEmployeeAlreadyLinked_DoesNotChangeAccount()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<
                    HrManagementDbContext>()
                .UseSqlite(
                    connection)
                .Options;

        Guid employeeId =
            Guid.NewGuid();

        Guid existingAccountId =
            Guid.NewGuid();

        Guid targetAccountId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(
                         options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Employees.Add(
                CreateEmployee(
                    employeeId,
                    "EMP001",
                    "Nguyễn Văn An"));

            dbContext.UserAccounts.AddRange(
                new UserAccount(
                    existingAccountId,
                    "existing",
                    "Existing",
                    UserAccountKind.Standard,
                    employeeId),
                new UserAccount(
                    targetAccountId,
                    "target",
                    "Target",
                    UserAccountKind.Standard));

            await dbContext.SaveChangesAsync();
        }

        var persistence =
            new EfAccountProfileUpdatePersistence(
                new TestDbContextFactory(
                    options));

        AccountProfileUpdatePersistenceResult result =
            await persistence.TryUpdateAsync(
                targetAccountId,
                "Target Updated",
                employeeId);

        Assert.Equal(
            AccountProfileUpdatePersistenceResult
                .EmployeeAlreadyLinked,
            result);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        UserAccount target =
            await verificationContext
                .UserAccounts
                .AsNoTracking()
                .SingleAsync(
                    account =>
                        account.Id ==
                        targetAccountId);

        Assert.Equal(
            "Target",
            target.DisplayName);

        Assert.Null(
            target.EmployeeId);
    }

    private static Employee CreateEmployee(
        Guid id,
        string employeeCode,
        string fullName)
    {
        return new Employee(
            id,
            employeeCode,
            fullName,
            null,
            null,
            null,
            new DateOnly(
                2026,
                1,
                1),
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);
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
