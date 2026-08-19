using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
public sealed class EfEmployeeAddressRepositoryTests
{
    // Không có địa chỉ → trả collection rỗng
    [Fact]
    public async Task GetByEmployeeIdAsync_WhenNoneExist_ReturnsEmpty()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        var repository =
            CreateRepository(
                options);

        IReadOnlyList<EmployeeAddress> addresses =
            await repository
                .GetByEmployeeIdAsync(
                    Guid.NewGuid());

        Assert.Empty(
            addresses);
    }

    // Upsert địa chỉ mới → insert thành công
    [Fact]
    public async Task UpsertAsync_WhenNew_AddsAddress()
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

        Guid addressId =
            Guid.NewGuid();

        await repository.UpsertAsync(
            new EmployeeAddress(
                addressId,
                employeeId,
                EmployeeAddressType.Current,
                "123 Nguyễn Trãi",
                ward:
                    "Phường Bến Thành",
                district:
                    "Quận 1",
                province:
                    "TP. Hồ Chí Minh",
                country:
                    "Việt Nam",
                postalCode:
                    "700000"));

        IReadOnlyList<EmployeeAddress> addresses =
            await repository
                .GetByEmployeeIdAsync(
                    employeeId);

        EmployeeAddress saved =
            Assert.Single(
                addresses);

        Assert.Equal(
            addressId,
            saved.Id);

        Assert.Equal(
            employeeId,
            saved.EmployeeId);

        Assert.Equal(
            EmployeeAddressType.Current,
            saved.Type);

        Assert.Equal(
            "123 Nguyễn Trãi",
            saved.AddressLine);

        Assert.Equal(
            "Phường Bến Thành",
            saved.Ward);

        Assert.Equal(
            "Quận 1",
            saved.District);

        Assert.Equal(
            "TP. Hồ Chí Minh",
            saved.Province);

        Assert.Equal(
            "Việt Nam",
            saved.Country);

        Assert.Equal(
            "700000",
            saved.PostalCode);
    }

    [Fact]
    public async Task UpsertAsync_WhenTypeExists_UpdatesAndPreservesId()
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

        Guid originalAddressId =
            Guid.NewGuid();

        await repository.UpsertAsync(
            new EmployeeAddress(
                originalAddressId,
                employeeId,
                EmployeeAddressType.Current,
                "Địa chỉ cũ",
                province:
                    "Hà Nội"));

        await repository.UpsertAsync(
            new EmployeeAddress(
                Guid.NewGuid(),
                employeeId,
                EmployeeAddressType.Current,
                "Địa chỉ mới",
                province:
                    "Đà Nẵng"));

        IReadOnlyList<EmployeeAddress> addresses =
            await repository
                .GetByEmployeeIdAsync(
                    employeeId);

        EmployeeAddress saved =
            Assert.Single(
                addresses);

        Assert.Equal(
            originalAddressId,
            saved.Id);

        Assert.Equal(
            "Địa chỉ mới",
            saved.AddressLine);

        Assert.Equal(
            "Đà Nẵng",
            saved.Province);
    }

    [Fact]
    public async Task UpsertAsync_AllowsPermanentAndCurrentForSameEmployee()
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

        IEmployeeAddressRepository repository =
            CreateRepository(
                options);

        await repository.UpsertAsync(
            new EmployeeAddress(
                Guid.NewGuid(),
                employeeId,
                EmployeeAddressType.Permanent,
                "Địa chỉ thường trú"));

        await repository.UpsertAsync(
            new EmployeeAddress(
                Guid.NewGuid(),
                employeeId,
                EmployeeAddressType.Current,
                "Địa chỉ hiện tại"));

        IReadOnlyList<EmployeeAddress> addresses =
            await repository
                .GetByEmployeeIdAsync(
                    employeeId);

        Assert.Equal(
            2,
            addresses.Count);

        Assert.Contains(
            addresses,
            address =>
                address.Type ==
                EmployeeAddressType.Permanent);

        Assert.Contains(
            addresses,
            address =>
                address.Type ==
                EmployeeAddressType.Current);
    }

    // Delete chỉ xóa đúng loại địa chỉ
    [Fact]
    public async Task DeleteAsync_RemovesOnlyRequestedType()
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
            new EmployeeAddress(
                Guid.NewGuid(),
                employeeId,
                EmployeeAddressType.Permanent,
                "Địa chỉ thường trú"));

        await repository.UpsertAsync(
            new EmployeeAddress(
                Guid.NewGuid(),
                employeeId,
                EmployeeAddressType.Current,
                "Địa chỉ hiện tại"));

        await repository.DeleteAsync(
            employeeId,
            EmployeeAddressType.Current);

        IReadOnlyList<EmployeeAddress> addresses =
            await repository
                .GetByEmployeeIdAsync(
                    employeeId);

        EmployeeAddress remaining =
            Assert.Single(
                addresses);

        Assert.Equal(
            EmployeeAddressType.Permanent,
            remaining.Type);

        Assert.Equal(
            "Địa chỉ thường trú",
            remaining.AddressLine);
    }

    // Xóa Employee → cascade toàn bộ address
    [Fact]
    public async Task EmployeeDeletion_CascadesAddresses()
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
            new EmployeeAddress(
                Guid.NewGuid(),
                employeeId,
                EmployeeAddressType.Permanent,
                "Địa chỉ thường trú"));

        await repository.UpsertAsync(
            new EmployeeAddress(
                Guid.NewGuid(),
                employeeId,
                EmployeeAddressType.Current,
                "Địa chỉ hiện tại"));

        await using (
            HrManagementDbContext dbContext =
                new(options))
        {
            Employee employee =
                await dbContext
                    .Employees
                    .SingleAsync(
                        item =>
                            item.Id ==
                            employeeId);

            dbContext.Employees.Remove(
                employee);

            await dbContext.SaveChangesAsync();
        }

        IReadOnlyList<EmployeeAddress> addresses =
            await repository
                .GetByEmployeeIdAsync(
                    employeeId);

        Assert.Empty(
            addresses);
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

    private static IEmployeeAddressRepository
        CreateRepository(
            DbContextOptions<HrManagementDbContext> options)
    {
        return new EfEmployeeAddressRepository(
            new TestDbContextFactory(
                options),
            new TestAuditEntryFactory());
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
}
