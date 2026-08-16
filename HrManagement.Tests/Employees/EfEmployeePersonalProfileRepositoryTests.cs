using HrManagement.Application.Employees.Profiles;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;
using HrManagement.Infrastructure.Employees.Profiles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Employees;

public sealed class EfEmployeePersonalProfileRepositoryTests
{
    [Fact]
    public async Task GetByEmployeeIdAsync_WhenProfileDoesNotExist_ReturnsNull()
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

        EmployeePersonalProfile? result =
            await repository
                .GetByEmployeeIdAsync(
                    Guid.NewGuid());

        Assert.Null(
            result);
    }

    [Fact]
    public async Task UpsertAsync_WhenProfileDoesNotExist_AddsProfile()
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

        var profile =
            new EmployeePersonalProfile(
                employeeId,
                "An",
                EmployeeGender.Male,
                "Việt Nam",
                "Hà Nội");

        await repository.UpsertAsync(
            profile);

        EmployeePersonalProfile? saved =
            await repository
                .GetByEmployeeIdAsync(
                    employeeId);

        Assert.NotNull(
            saved);

        Assert.Equal(
            "An",
            saved.PreferredName);

        Assert.Equal(
            EmployeeGender.Male,
            saved.Gender);

        Assert.Equal(
            "Việt Nam",
            saved.Nationality);

        Assert.Equal(
            "Hà Nội",
            saved.PlaceOfBirth);
    }

    [Fact]
    public async Task UpsertAsync_WhenProfileExists_UpdatesProfile()
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

        await repository.UpsertAsync(
            new EmployeePersonalProfile(
                employeeId,
                "An Nguyễn",
                EmployeeGender.Male,
                "Việt Nam",
                "Đà Nẵng"));

        EmployeePersonalProfile? saved =
            await repository
                .GetByEmployeeIdAsync(
                    employeeId);

        Assert.NotNull(
            saved);

        Assert.Equal(
            "An Nguyễn",
            saved.PreferredName);

        Assert.Equal(
            "Đà Nẵng",
            saved.PlaceOfBirth);
    }

    [Fact]
    public async Task EmployeeDeletion_CascadesPersonalProfile()
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
                "Test"));

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

        EmployeePersonalProfile? saved =
            await repository
                .GetByEmployeeIdAsync(
                    employeeId);

        Assert.Null(
            saved);
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

    private static IEmployeePersonalProfileRepository
        CreateRepository(
            DbContextOptions<HrManagementDbContext> options)
    {
        return new EfEmployeePersonalProfileRepository(
            new TestDbContextFactory(
                options));
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
