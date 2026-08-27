using HrManagement.Domain.Overtime.Requests;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Tests.Overtime;

public sealed class OvertimePersistenceModelTests
{
    [Fact]
    public async Task SaveAsync_PersistsRequestAndStatusHistory()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        OvertimeRequest request =
            CreateRequest();

        OvertimeRequestStatusChange change =
            request.TransitionTo(
                Guid.NewGuid(),
                OvertimeRequestStatus.Approved,
                Utc(
                    13),
                "user-1",
                "admin",
                approvedMinutes: 90,
                note:
                    "Duyệt một phần");

        await using (
            HrManagementDbContext dbContext =
                await database.CreateContextAsync())
        {
            dbContext.OvertimeRequests.Add(
                request);

            dbContext.OvertimeRequestStatusChanges.Add(
                change);

            await dbContext.SaveChangesAsync();
        }

        await using HrManagementDbContext verification =
            await database.CreateContextAsync();

        OvertimeRequest savedRequest =
            await verification
                .OvertimeRequests
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            savedRequest.Status);

        Assert.Equal(
            90,
            savedRequest.ApprovedMinutes);

        OvertimeRequestStatusChange savedChange =
            await verification
                .OvertimeRequestStatusChanges
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OvertimeRequestStatus.Pending,
            savedChange.PreviousStatus);

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            savedChange.NewStatus);

        Assert.Equal(
            90,
            savedChange.ApprovedMinutes);

        Assert.Equal(
            "Duyệt một phần",
            savedChange.Note);
    }

    [Fact]
    public async Task SaveAsync_WhenSecondActiveRequestExistsForSameEmployeeAndDate_IsRejected()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        Guid employeeId =
            Guid.NewGuid();

        DateOnly workDate =
            new(
                2026,
                8,
                27);

        OvertimeRequest first =
            CreateRequest(
                employeeId,
                workDate);

        await using (
            HrManagementDbContext dbContext =
                await database.CreateContextAsync())
        {
            dbContext.OvertimeRequests.Add(
                first);

            await dbContext.SaveChangesAsync();
        }

        OvertimeRequest second =
            CreateRequest(
                employeeId,
                workDate);

        await using HrManagementDbContext secondContext =
            await database.CreateContextAsync();

        secondContext.OvertimeRequests.Add(
            second);

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                secondContext.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveAsync_AfterRejectedRequest_AllowsNewPendingRequestForSameEmployeeAndDate()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        Guid employeeId =
            Guid.NewGuid();

        DateOnly workDate =
            new(
                2026,
                8,
                27);

        OvertimeRequest first =
            CreateRequest(
                employeeId,
                workDate);

        OvertimeRequestStatusChange rejection =
            first.TransitionTo(
                Guid.NewGuid(),
                OvertimeRequestStatus.Rejected,
                Utc(
                    13),
                "user-1",
                "admin");

        await using (
            HrManagementDbContext dbContext =
                await database.CreateContextAsync())
        {
            dbContext.OvertimeRequests.Add(
                first);

            dbContext.OvertimeRequestStatusChanges.Add(
                rejection);

            await dbContext.SaveChangesAsync();
        }

        OvertimeRequest replacement =
            CreateRequest(
                employeeId,
                workDate);

        await using (
            HrManagementDbContext dbContext =
                await database.CreateContextAsync())
        {
            dbContext.OvertimeRequests.Add(
                replacement);

            await dbContext.SaveChangesAsync();
        }

        await using HrManagementDbContext verification =
            await database.CreateContextAsync();

        OvertimeRequest[] requests =
            await verification
                .OvertimeRequests
                .AsNoTracking()
                .OrderBy(
                    request =>
                        request.SubmittedAtUtc)
                .ToArrayAsync();

        Assert.Equal(
            2,
            requests.Length);

        Assert.Contains(
            requests,
            request =>
                request.Status ==
                OvertimeRequestStatus.Rejected);

        Assert.Contains(
            requests,
            request =>
                request.Status ==
                OvertimeRequestStatus.Pending);
    }

    [Fact]
    public async Task SaveAsync_ActiveRequestsOnDifferentDates_AreAllowed()
    {
        await using TestDatabase database =
            await TestDatabase.CreateAsync();

        Guid employeeId =
            Guid.NewGuid();

        OvertimeRequest first =
            CreateRequest(
                employeeId,
                new DateOnly(
                    2026,
                    8,
                    27));

        OvertimeRequest second =
            CreateRequest(
                employeeId,
                new DateOnly(
                    2026,
                    8,
                    28));

        await using HrManagementDbContext dbContext =
            await database.CreateContextAsync();

        dbContext.OvertimeRequests.AddRange(
            first,
            second);

        await dbContext.SaveChangesAsync();

        Assert.Equal(
            2,
            await dbContext
                .OvertimeRequests
                .CountAsync());
    }

    private static OvertimeRequest CreateRequest(
        Guid? employeeId = null,
        DateOnly? workDate = null)
    {
        return new OvertimeRequest(
            Guid.NewGuid(),
            employeeId
                ?? Guid.NewGuid(),
            Guid.NewGuid(),
            workDate
                ?? new DateOnly(
                    2026,
                    8,
                    27),
            requestedMinutes:
                120,
            reason:
                "Kiểm thử tăng ca",
            submittedAtUtc:
                Utc());
    }

    private static DateTime Utc(
        int hour = 12)
    {
        return new DateTime(
            2026,
            8,
            27,
            hour,
            0,
            0,
            DateTimeKind.Utc);
    }

    private sealed class TestDatabase
        : IAsyncDisposable
    {
        private readonly SqliteConnection
            _connection;

        private readonly DbContextOptions<
            HrManagementDbContext>
            _options;

        private TestDatabase(
            SqliteConnection connection,
            DbContextOptions<HrManagementDbContext> options)
        {
            _connection =
                connection;

            _options =
                options;
        }

        public static async Task<TestDatabase>
            CreateAsync()
        {
            var connection =
                new SqliteConnection(
                    "Data Source=:memory:;Foreign Keys=False");

            await connection.OpenAsync();

            DbContextOptions<HrManagementDbContext> options =
                new DbContextOptionsBuilder<
                    HrManagementDbContext>()
                    .UseSqlite(
                        connection)
                    .Options;

            await using var dbContext =
                new HrManagementDbContext(
                    options);

            await dbContext.Database
                .EnsureCreatedAsync();

            return new TestDatabase(
                connection,
                options);
        }

        public Task<HrManagementDbContext>
            CreateContextAsync()
        {
            return Task.FromResult(
                new HrManagementDbContext(
                    _options));
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}
