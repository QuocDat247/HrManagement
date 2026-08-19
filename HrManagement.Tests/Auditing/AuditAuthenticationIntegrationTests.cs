using HrManagement.Application.Auditing;
using HrManagement.Application.Authentication;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;
using HrManagement.Infrastructure.Authentication;
using HrManagement.Infrastructure.Employees.Profiles;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Auditing;

public sealed class AuditAuthenticationIntegrationTests
{
    [Fact]
    public async Task AuthenticatedLogin_ProfileMutation_PersistsCurrentActorAndUtcTimestamp()
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

        var session =
            new CurrentUserSession();

        var authenticationService =
            new FakeAuthenticationService(
                session);

        AuthenticationResult loginResult =
            await authenticationService.LoginAsync(
                "admin",
                "admin123");

        Assert.True(
            loginResult.IsSuccessful);

        DateTimeOffset utcNow =
            new DateTimeOffset(
                2026,
                8,
                19,
                6,
                30,
                0,
                TimeSpan.Zero);

        var auditEntryFactory =
            new AuditEntryFactory(
                session,
                new StubTimeProvider(
                    utcNow));

        var repository =
            new EfEmployeePersonalProfileRepository(
                new TestDbContextFactory(
                    options),
                auditEntryFactory);

        await repository.UpsertAsync(
            new EmployeePersonalProfile(
                employeeId,
                "An",
                EmployeeGender.Male,
                "Việt Nam",
                "Hà Nội"));

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        EmployeePersonalProfile savedProfile =
            await verificationContext
                .EmployeePersonalProfiles
                .AsNoTracking()
                .SingleAsync();

        AuditEntry audit =
            await verificationContext
                .AuditEntries
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            employeeId,
            savedProfile.EmployeeId);

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

        Assert.Equal(
            "demo-admin",
            audit.ActorUserId);

        Assert.Equal(
            "admin",
            audit.ActorUsername);

        Assert.Equal(
            utcNow.UtcDateTime,
            audit.OccurredAtUtc);

        Assert.Equal(
            DateTimeKind.Utc,
            audit.OccurredAtUtc.Kind);
    }

    [Fact]
    public async Task ProfileMutation_WhenNotAuthenticated_IsRejectedAndDoesNotPersistData()
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

        var session =
            new CurrentUserSession();

        var auditEntryFactory =
            new AuditEntryFactory(
                session,
                new StubTimeProvider(
                    new DateTimeOffset(
                        2026,
                        8,
                        19,
                        6,
                        30,
                        0,
                        TimeSpan.Zero)));

        var repository =
            new EfEmployeePersonalProfileRepository(
                new TestDbContextFactory(
                    options),
                auditEntryFactory);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    repository.UpsertAsync(
                        new EmployeePersonalProfile(
                            employeeId,
                            "Không được lưu",
                            EmployeeGender.Male,
                            "Việt Nam",
                            "Hà Nội")));

        Assert.Equal(
            "Không thể tạo audit khi chưa có người dùng đăng nhập.",
            exception.Message);

        await using var verificationContext =
            new HrManagementDbContext(
                options);

        Assert.Empty(
            await verificationContext
                .EmployeePersonalProfiles
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await verificationContext
                .AuditEntries
                .AsNoTracking()
                .ToListAsync());
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

    private sealed class StubTimeProvider
        : TimeProvider
    {
        private readonly DateTimeOffset
            _utcNow;

        public StubTimeProvider(
            DateTimeOffset utcNow)
        {
            _utcNow =
                utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}
