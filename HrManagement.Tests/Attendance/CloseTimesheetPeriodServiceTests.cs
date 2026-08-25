using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Application.Authentication;

namespace HrManagement.Tests.Attendance;

public sealed class CloseTimesheetPeriodServiceTests
{
    [Fact]
    public async Task CloseAsync_WhenUserIsNotAuthenticated_Fails()
    {
        var persistence =
            new StubClosePersistence();

        var service =
            CreateService(
                persistence,
                authenticated: false);

        CloseTimesheetPeriodResult result =
            await service.CloseAsync(
                new CloseTimesheetPeriodRequest(
                    2026,
                    8));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Không thể đóng kỳ công khi chưa có người dùng đăng nhập.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            persistence.CallCount);
    }

    [Fact]
    public async Task CloseAsync_WhenAuthorizationIsDenied_Fails()
    {
        var persistence =
            new StubClosePersistence();

        var service =
            CreateService(
                persistence,
                authorizationAllowed: false);

        CloseTimesheetPeriodResult result =
            await service.CloseAsync(
                new CloseTimesheetPeriodRequest(
                    2026,
                    8));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Bạn không có quyền đóng kỳ công.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            persistence.CallCount);
    }

    [Fact]
    public async Task CloseAsync_WhenMonthIsInvalid_FailsBeforePersistence()
    {
        var persistence =
            new StubClosePersistence();

        var service =
            CreateService(
                persistence);

        CloseTimesheetPeriodResult result =
            await service.CloseAsync(
                new CloseTimesheetPeriodRequest(
                    2026,
                    13));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Tháng kỳ công phải từ 1 đến 12.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            persistence.CallCount);
    }

    [Fact]
    public async Task CloseAsync_WhenSuccessful_PassesActorAndUtcTime()
    {
        Guid periodId =
            Guid.NewGuid();

        var persistence =
            new StubClosePersistence
            {
                Result =
                    new CloseTimesheetPeriodPersistenceResult(
                        periodId,
                        31)
            };

        DateTimeOffset now =
            new(
                2026,
                8,
                31,
                17,
                30,
                0,
                TimeSpan.Zero);

        var service =
            CreateService(
                persistence,
                now:
                    now);

        CloseTimesheetPeriodResult result =
            await service.CloseAsync(
                new CloseTimesheetPeriodRequest(
                    2026,
                    8));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            periodId,
            result.TimesheetPeriodId);

        Assert.Equal(
            31,
            result.SnapshotCount);

        Assert.Equal(
            now.UtcDateTime,
            result.ClosedAtUtc);

        Assert.Equal(
            now.UtcDateTime,
            persistence.ClosedAtUtc);

        Assert.Equal(
            "user-1",
            persistence.ActorUserId);

        Assert.Equal(
            "admin",
            persistence.ActorUsername);

        Assert.Equal(
            1,
            persistence.CallCount);
    }

    [Fact]
    public async Task CloseAsync_WhenPersistenceRejectsClose_ReturnsFailure()
    {
        var persistence =
            new StubClosePersistence
            {
                Exception =
                    new InvalidOperationException(
                        "Kỳ công đã được đóng.")
            };

        var service =
            CreateService(
                persistence);

        CloseTimesheetPeriodResult result =
            await service.CloseAsync(
                new CloseTimesheetPeriodRequest(
                    2026,
                    8));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Kỳ công đã được đóng.",
            result.ErrorMessage);
    }

    private static CloseTimesheetPeriodService CreateService(
        StubClosePersistence persistence,
        bool authenticated = true,
        bool authorizationAllowed = true,
        DateTimeOffset? now = null)
    {
        AuthenticatedUser? currentUser =
            authenticated
                ? new AuthenticatedUser(
                    "user-1",
                    "admin",
                    "Administrator")
                : null;

        return new CloseTimesheetPeriodService(
            persistence,
            new StubCurrentUserContext(
                currentUser),
            new StubAuthorizationPolicy(
                authorizationAllowed),
            new StubTimeProvider(
                now
                ?? new DateTimeOffset(
                    2026,
                    8,
                    31,
                    12,
                    0,
                    0,
                    TimeSpan.Zero)));
    }

    private sealed class StubClosePersistence
        : ICloseTimesheetPeriodPersistence
    {
        public CloseTimesheetPeriodPersistenceResult Result
        {
            get;
            init;
        } =
            new(
                Guid.NewGuid(),
                1);

        public Exception? Exception
        {
            get;
            init;
        }

        public int CallCount
        {
            get;
            private set;
        }

        public DateTime? ClosedAtUtc
        {
            get;
            private set;
        }

        public string? ActorUserId
        {
            get;
            private set;
        }

        public string? ActorUsername
        {
            get;
            private set;
        }

        public Task<CloseTimesheetPeriodPersistenceResult> CloseAsync(
            int year,
            int month,
            DateTime closedAtUtc,
            string actorUserId,
            string actorUsername,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            ClosedAtUtc =
                closedAtUtc;

            ActorUserId =
                actorUserId;

            ActorUsername =
                actorUsername;

            if (Exception is not null)
            {
                throw Exception;
            }

            return Task.FromResult(
                Result);
        }
    }

    private sealed class StubCurrentUserContext
        : ICurrentUserContext
    {
        public AuthenticatedUser? CurrentUser
        {
            get;
        }

        public bool IsAuthenticated =>
            CurrentUser is not null;

        public StubCurrentUserContext(
            AuthenticatedUser? currentUser)
        {
            CurrentUser =
                currentUser;
        }
    }

    private sealed class StubAuthorizationPolicy
        : ITimesheetPeriodClosingAuthorizationPolicy
    {
        private readonly bool
            _allowed;

        public StubAuthorizationPolicy(
            bool allowed)
        {
            _allowed =
                allowed;
        }

        public Task<bool> CanCloseAsync(
            TimesheetPeriodClosingAuthorizationRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _allowed);
        }
    }

    private sealed class StubTimeProvider
        : TimeProvider
    {
        private readonly DateTimeOffset
            _now;

        public StubTimeProvider(
            DateTimeOffset now)
        {
            _now =
                now;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }
    }
}
