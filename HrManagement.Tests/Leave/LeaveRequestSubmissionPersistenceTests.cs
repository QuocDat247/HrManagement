using HrManagement.Application.Leave.Requests;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Leave.Requests;
using HrManagement.Domain.Leave.Types;
using HrManagement.Infrastructure.Leave.Requests;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Leave;

public sealed class LeaveRequestSubmissionPersistenceTests
{
    [Fact]
    public async Task ValidPendingRequest_IsPersisted()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedContextAsync(
                options);

        LeaveRequest request =
            CreateRequest(
                seed,
                new DateOnly(
                    2026,
                    8,
                    20),
                new DateOnly(
                    2026,
                    8,
                    22));

        ILeaveRequestSubmissionPersistence persistence =
            CreatePersistence(
                options);

        await persistence.SubmitAsync(
            request);

        await using var verification =
            new HrManagementDbContext(
                options);

        LeaveRequest saved =
            await verification
                .LeaveRequests
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            request.Id,
            saved.Id);

        Assert.Equal(
            seed.EmployeeId,
            saved.EmployeeId);

        Assert.Equal(
            seed.EmploymentPeriodId,
            saved.EmploymentPeriodId);

        Assert.Equal(
            LeaveRequestStatus.Pending,
            saved.Status);
    }

    [Fact]
    public async Task OverlapInsertedAfterApplicationCheck_ThrowsConcurrency()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedContextAsync(
                options);

        LeaveRequest candidate =
            CreateRequest(
                seed,
                new DateOnly(
                    2026,
                    8,
                    20),
                new DateOnly(
                    2026,
                    8,
                    22));

        LeaveRequest competing =
            CreateRequest(
                seed,
                new DateOnly(
                    2026,
                    8,
                    22),
                new DateOnly(
                    2026,
                    8,
                    24));

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext
                .LeaveRequests
                .AddAsync(
                    competing);

            await dbContext.SaveChangesAsync();
        }

        ILeaveRequestSubmissionPersistence persistence =
            CreatePersistence(
                options);

        await Assert.ThrowsAsync<
            DbUpdateConcurrencyException>(
                () =>
                    persistence.SubmitAsync(
                        candidate));

        await using var verification =
            new HrManagementDbContext(
                options);

        List<LeaveRequest> saved =
            await verification
                .LeaveRequests
                .AsNoTracking()
                .ToListAsync();

        Assert.Single(
            saved);

        Assert.Equal(
            competing.Id,
            saved[0].Id);
    }

    [Fact]
    public async Task AdjacentRequest_IsAllowed()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedContextAsync(
                options);

        LeaveRequest existing =
            CreateRequest(
                seed,
                new DateOnly(
                    2026,
                    8,
                    20),
                new DateOnly(
                    2026,
                    8,
                    22));

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext
                .LeaveRequests
                .AddAsync(
                    existing);

            await dbContext.SaveChangesAsync();
        }

        LeaveRequest adjacent =
            CreateRequest(
                seed,
                new DateOnly(
                    2026,
                    8,
                    23),
                new DateOnly(
                    2026,
                    8,
                    24));

        ILeaveRequestSubmissionPersistence persistence =
            CreatePersistence(
                options);

        await persistence.SubmitAsync(
            adjacent);

        await using var verification =
            new HrManagementDbContext(
                options);

        Assert.Equal(
            2,
            await verification
                .LeaveRequests
                .CountAsync());
    }

    [Fact]
    public async Task OverlapForDifferentEmployee_DoesNotBlock()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext first =
            await SeedContextAsync(
                options,
                "ANNUAL");

        SeedContext second =
            await SeedContextAsync(
                options,
                "OTHER");

        LeaveRequest existing =
            CreateRequest(
                first,
                new DateOnly(
                    2026,
                    8,
                    20),
                new DateOnly(
                    2026,
                    8,
                    22));

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext
                .LeaveRequests
                .AddAsync(
                    existing);

            await dbContext.SaveChangesAsync();
        }

        LeaveRequest secondEmployeeRequest =
            CreateRequest(
                second,
                new DateOnly(
                    2026,
                    8,
                    20),
                new DateOnly(
                    2026,
                    8,
                    22));

        ILeaveRequestSubmissionPersistence persistence =
            CreatePersistence(
                options);

        await persistence.SubmitAsync(
            secondEmployeeRequest);

        await using var verification =
            new HrManagementDbContext(
                options);

        Assert.Equal(
            2,
            await verification
                .LeaveRequests
                .CountAsync());
    }

    [Fact]
    public async Task PersistedEmploymentPeriodNoLongerCoversRange_ThrowsConcurrency()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedContextAsync(
                options,
                leaveTypeCode: "ANNUAL",
                periodEndDate:
                    new DateOnly(
                        2026,
                        8,
                        21));

        LeaveRequest staleCandidate =
            CreateRequest(
                seed,
                new DateOnly(
                    2026,
                    8,
                    20),
                new DateOnly(
                    2026,
                    8,
                    22));

        ILeaveRequestSubmissionPersistence persistence =
            CreatePersistence(
                options);

        await Assert.ThrowsAsync<
            DbUpdateConcurrencyException>(
                () =>
                    persistence.SubmitAsync(
                        staleCandidate));

        await using var verification =
            new HrManagementDbContext(
                options);

        Assert.Empty(
            await verification
                .LeaveRequests
                .ToListAsync());
    }

    private static ILeaveRequestSubmissionPersistence
        CreatePersistence(
            DbContextOptions<HrManagementDbContext> options)
    {
        return new EfLeaveRequestSubmissionPersistence(
            new TestDbContextFactory(
                options));
    }

    private static LeaveRequest CreateRequest(
        SeedContext seed,
        DateOnly startDate,
        DateOnly endDate)
    {
        return new LeaveRequest(
            Guid.NewGuid(),
            seed.EmployeeId,
            seed.EmploymentPeriodId,
            seed.LeaveTypeId,
            startDate,
            endDate,
            null,
            new DateTime(
                2026,
                8,
                10,
                3,
                0,
                0,
                DateTimeKind.Utc));
    }

    private static async Task<SeedContext> SeedContextAsync(
        DbContextOptions<HrManagementDbContext> options,
        string leaveTypeCode = "ANNUAL",
        DateOnly? periodEndDate = null)
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid employmentPeriodId =
            Guid.NewGuid();

        Guid leaveTypeId =
            Guid.NewGuid();

        await using var dbContext =
            new HrManagementDbContext(
                options);

        await dbContext.Employees.AddAsync(
            new Employee(
                employeeId,
                $"EMP{employeeId:N}"[..20],
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

        await dbContext.EmploymentPeriods.AddAsync(
            new EmploymentPeriod(
                employmentPeriodId,
                employeeId,
                new DateOnly(
                    2026,
                    1,
                    1),
                periodEndDate));

        await dbContext.LeaveTypes.AddAsync(
            new LeaveType(
                leaveTypeId,
                leaveTypeCode,
                $"Loại nghỉ {leaveTypeCode}",
                isPaid: true));

        await dbContext.SaveChangesAsync();

        return new SeedContext(
            employeeId,
            employmentPeriodId,
            leaveTypeId);
    }

    private static async Task<SqliteConnection>
        CreateOpenConnectionAsync()
    {
        var connection =
            new SqliteConnection(
                "Data Source=:memory:;Foreign Keys=True");

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

    private sealed record SeedContext(
        Guid EmployeeId,
        Guid EmploymentPeriodId,
        Guid LeaveTypeId);

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
                CreateDbContext());
        }
    }

    // Rejected không block
    [Fact]
    public async Task RejectedOverlap_DoesNotBlockSubmission()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedContextAsync(
                options);

        LeaveRequest existing =
            CreateRequest(
                seed,
                new DateOnly(
                    2026,
                    8,
                    20),
                new DateOnly(
                    2026,
                    8,
                    22));

        LeaveRequestStatusChange rejection =
            existing.TransitionTo(
                Guid.NewGuid(),
                LeaveRequestStatus.Rejected,
                new DateTime(
                    2026,
                    8,
                    19,
                    5,
                    0,
                    0,
                    DateTimeKind.Utc),
                "user-001",
                "admin");

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext
                .LeaveRequests
                .AddAsync(
                    existing);

            await dbContext
                .LeaveRequestStatusChanges
                .AddAsync(
                    rejection);

            await dbContext.SaveChangesAsync();
        }

        LeaveRequest candidate =
            CreateRequest(
                seed,
                new DateOnly(
                    2026,
                    8,
                    21),
                new DateOnly(
                    2026,
                    8,
                    23));

        ILeaveRequestSubmissionPersistence persistence =
            CreatePersistence(
                options);

        await persistence.SubmitAsync(
            candidate);

        await using var verification =
            new HrManagementDbContext(
                options);

        Assert.Equal(
            2,
            await verification
                .LeaveRequests
                .CountAsync());
    }

    // Cancelled không block
    [Fact]
    public async Task CancelledOverlap_DoesNotBlockSubmission()
    {
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync();

        DbContextOptions<HrManagementDbContext> options =
            CreateOptions(
                connection);

        await EnsureCreatedAsync(
            options);

        SeedContext seed =
            await SeedContextAsync(
                options);

        LeaveRequest existing =
            CreateRequest(
                seed,
                new DateOnly(
                    2026,
                    8,
                    20),
                new DateOnly(
                    2026,
                    8,
                    22));

        LeaveRequestStatusChange cancellation =
            existing.TransitionTo(
                Guid.NewGuid(),
                LeaveRequestStatus.Cancelled,
                new DateTime(
                    2026,
                    8,
                    19,
                    5,
                    0,
                    0,
                    DateTimeKind.Utc),
                "user-001",
                "admin");

        await using (
            var dbContext =
                new HrManagementDbContext(
                    options))
        {
            await dbContext
                .LeaveRequests
                .AddAsync(
                    existing);

            await dbContext
                .LeaveRequestStatusChanges
                .AddAsync(
                    cancellation);

            await dbContext.SaveChangesAsync();
        }

        LeaveRequest candidate =
            CreateRequest(
                seed,
                new DateOnly(
                    2026,
                    8,
                    21),
                new DateOnly(
                    2026,
                    8,
                    23));

        ILeaveRequestSubmissionPersistence persistence =
            CreatePersistence(
                options);

        await persistence.SubmitAsync(
            candidate);

        await using var verification =
            new HrManagementDbContext(
                options);

        Assert.Equal(
            2,
            await verification
                .LeaveRequests
                .CountAsync());
    }
}
