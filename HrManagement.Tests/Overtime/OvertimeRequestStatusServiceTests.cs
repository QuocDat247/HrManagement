using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Application.Authentication;
using HrManagement.Application.Overtime.Requests;
using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Tests.Overtime;

public sealed class OvertimeRequestStatusServiceTests
{
    [Fact]
    public async Task ChangeStatusAsync_Approve_PersistsPartialApproval()
    {
        OvertimeRequest overtimeRequest =
            CreatePendingRequest(
                requestedMinutes:
                    120);

        TestContext context =
            CreateContext(
                overtimeRequest);

        ChangeOvertimeRequestStatusResult result =
            await context.Service.ChangeStatusAsync(
                new ChangeOvertimeRequestStatusRequest(
                    overtimeRequest.Id,
                    OvertimeRequestStatus.Pending,
                    OvertimeRequestStatus.Approved,
                    ApprovedMinutes:
                        90,
                    Note:
                        "Duyệt một phần"));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            result.Status);

        Assert.Equal(
            90,
            result.ApprovedMinutes);

        OvertimeRequestStatusChange change =
            Assert.Single(
                context.Persistence.Applied);

        Assert.Equal(
            OvertimeRequestStatus.Pending,
            change.PreviousStatus);

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            change.NewStatus);

        Assert.Equal(
            90,
            change.ApprovedMinutes);

        Assert.Equal(
            "user-1",
            context.Persistence.LastActorUserId);
    }

    [Fact]
    public async Task ChangeStatusAsync_Reject_PersistsTransition()
    {
        OvertimeRequest overtimeRequest =
            CreatePendingRequest();

        TestContext context =
            CreateContext(
                overtimeRequest);

        ChangeOvertimeRequestStatusResult result =
            await context.Service.ChangeStatusAsync(
                new ChangeOvertimeRequestStatusRequest(
                    overtimeRequest.Id,
                    OvertimeRequestStatus.Pending,
                    OvertimeRequestStatus.Rejected,
                    Note:
                        "Không cần tăng ca"));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            OvertimeRequestStatus.Rejected,
            result.Status);

        Assert.Null(
            result.ApprovedMinutes);

        Assert.Single(
            context.Persistence.Applied);
    }

    [Fact]
    public async Task ChangeStatusAsync_CancelApproved_ClearsApprovedMinutes()
    {
        OvertimeRequest overtimeRequest =
            CreatePendingRequest(
                requestedMinutes:
                    120);

        overtimeRequest.TransitionTo(
            Guid.NewGuid(),
            OvertimeRequestStatus.Approved,
            Utc(
                12),
            "user-1",
            "admin",
            approvedMinutes:
                120);

        TestContext context =
            CreateContext(
                overtimeRequest);

        ChangeOvertimeRequestStatusResult result =
            await context.Service.ChangeStatusAsync(
                new ChangeOvertimeRequestStatusRequest(
                    overtimeRequest.Id,
                    OvertimeRequestStatus.Approved,
                    OvertimeRequestStatus.Cancelled));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            OvertimeRequestStatus.Cancelled,
            result.Status);

        Assert.Null(
            result.ApprovedMinutes);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenNotAuthenticated_FailsBeforeAuthorization()
    {
        OvertimeRequest overtimeRequest =
            CreatePendingRequest();

        TestContext context =
            CreateContext(
                overtimeRequest);

        context.CurrentUserContext.CurrentUser =
            null;

        ChangeOvertimeRequestStatusResult result =
            await context.Service.ChangeStatusAsync(
                ValidApproveRequest(
                    overtimeRequest));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            0,
            context.AuthorizationPolicy.CallCount);

        Assert.Equal(
            0,
            context.ContextSource.CallCount);

        Assert.Empty(
            context.Persistence.Applied);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenUnauthorized_FailsBeforeLookup()
    {
        OvertimeRequest overtimeRequest =
            CreatePendingRequest();

        TestContext context =
            CreateContext(
                overtimeRequest);

        context.AuthorizationPolicy.Result =
            false;

        ChangeOvertimeRequestStatusResult result =
            await context.Service.ChangeStatusAsync(
                ValidApproveRequest(
                    overtimeRequest));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            0,
            context.ContextSource.CallCount);

        Assert.Empty(
            context.Persistence.Applied);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenExpectedStatusIsStale_FailsBeforePeriodLock()
    {
        OvertimeRequest overtimeRequest =
            CreatePendingRequest();

        TestContext context =
            CreateContext(
                overtimeRequest);

        ChangeOvertimeRequestStatusResult result =
            await context.Service.ChangeStatusAsync(
                new ChangeOvertimeRequestStatusRequest(
                    overtimeRequest.Id,
                    OvertimeRequestStatus.Approved,
                    OvertimeRequestStatus.Cancelled));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Yêu cầu tăng ca đã thay đổi trạng thái. Vui lòng làm mới dữ liệu trước khi thao tác.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            context.PeriodLockPolicy.CallCount);

        Assert.Empty(
            context.Persistence.Applied);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenPeriodIsClosed_FailsBeforePersistence()
    {
        OvertimeRequest overtimeRequest =
            CreatePendingRequest();

        TestContext context =
            CreateContext(
                overtimeRequest);

        context.PeriodLockPolicy.IsLocked =
            true;

        ChangeOvertimeRequestStatusResult result =
            await context.Service.ChangeStatusAsync(
                ValidApproveRequest(
                    overtimeRequest));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Kỳ công của ngày tăng ca đã được đóng. Không thể thay đổi trạng thái yêu cầu tăng ca.",
            result.ErrorMessage);

        Assert.Empty(
            context.Persistence.Applied);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenApprovedMinutesExceedRequested_FailsWithoutPersistence()
    {
        OvertimeRequest overtimeRequest =
            CreatePendingRequest(
                requestedMinutes:
                    60);

        TestContext context =
            CreateContext(
                overtimeRequest);

        ChangeOvertimeRequestStatusResult result =
            await context.Service.ChangeStatusAsync(
                new ChangeOvertimeRequestStatusRequest(
                    overtimeRequest.Id,
                    OvertimeRequestStatus.Pending,
                    OvertimeRequestStatus.Approved,
                    ApprovedMinutes:
                        90));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Số phút tăng ca được duyệt phải từ 1 đến số phút đã yêu cầu.",
            result.ErrorMessage);

        Assert.Empty(
            context.Persistence.Applied);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenPersistenceDetectsConcurrency_ReturnsFriendlyFailure()
    {
        OvertimeRequest overtimeRequest =
            CreatePendingRequest();

        TestContext context =
            CreateContext(
                overtimeRequest);

        context.Persistence.ExceptionToThrow =
            new OvertimeRequestStatusConcurrencyException(
                "Yêu cầu tăng ca đã thay đổi trạng thái trước khi lưu. Vui lòng làm mới dữ liệu.");

        ChangeOvertimeRequestStatusResult result =
            await context.Service.ChangeStatusAsync(
                ValidApproveRequest(
                    overtimeRequest));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Yêu cầu tăng ca đã thay đổi trạng thái trước khi lưu. Vui lòng làm mới dữ liệu.",
            result.ErrorMessage);
    }

    private static ChangeOvertimeRequestStatusRequest
        ValidApproveRequest(
            OvertimeRequest overtimeRequest)
    {
        return new ChangeOvertimeRequestStatusRequest(
            overtimeRequest.Id,
            OvertimeRequestStatus.Pending,
            OvertimeRequestStatus.Approved,
            ApprovedMinutes:
                overtimeRequest.RequestedMinutes);
    }

    private static TestContext CreateContext(
        OvertimeRequest overtimeRequest)
    {
        var contextSource =
            new StubContextSource
            {
                Request =
                    overtimeRequest
            };

        var persistence =
            new StubPersistence();

        var authorizationPolicy =
            new StubAuthorizationPolicy();

        var periodLockPolicy =
            new StubPeriodLockPolicy();

        var currentUserContext =
            new StubCurrentUserContext
            {
                CurrentUser =
                    new AuthenticatedUser(
                        "user-1",
                        "admin",
                        "Administrator")
            };

        var service =
            new OvertimeRequestStatusService(
                contextSource,
                persistence,
                authorizationPolicy,
                periodLockPolicy,
                currentUserContext,
                new FixedTimeProvider(
                    new DateTimeOffset(
                        Utc(
                            13))));

        return new TestContext(
            service,
            contextSource,
            persistence,
            authorizationPolicy,
            periodLockPolicy,
            currentUserContext);
    }

    private static OvertimeRequest CreatePendingRequest(
        int requestedMinutes = 120)
    {
        return new OvertimeRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(
                2026,
                8,
                27),
            requestedMinutes,
            "Kiểm thử tăng ca",
            Utc(
                11));
    }

    private sealed record TestContext(
        OvertimeRequestStatusService Service,
        StubContextSource ContextSource,
        StubPersistence Persistence,
        StubAuthorizationPolicy AuthorizationPolicy,
        StubPeriodLockPolicy PeriodLockPolicy,
        StubCurrentUserContext CurrentUserContext);

    private sealed class StubContextSource
        : IOvertimeRequestStatusTransitionContextSource
    {
        public OvertimeRequest? Request
        {
            get;
            set;
        }

        public int CallCount
        {
            get;
            private set;
        }

        public Task<OvertimeRequest?> GetByIdAsync(
            Guid overtimeRequestId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(
                Request);
        }
    }

    private sealed class StubPersistence
        : IOvertimeRequestStatusTransitionPersistence
    {
        public Exception? ExceptionToThrow
        {
            get;
            set;
        }

        public List<OvertimeRequestStatusChange> Applied
        {
            get;
        } = [];

        public string? LastActorUserId
        {
            get;
            private set;
        }

        public string? LastActorUsername
        {
            get;
            private set;
        }

        public Task ApplyAsync(
            OvertimeRequestStatusChange statusChange,
            string actorUserId,
            string actorUsername,
            CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            Applied.Add(
                statusChange);

            LastActorUserId =
                actorUserId;

            LastActorUsername =
                actorUsername;

            return Task.CompletedTask;
        }
    }

    private sealed class StubAuthorizationPolicy
        : IOvertimeRequestStatusAuthorizationPolicy
    {
        public bool Result
        {
            get;
            set;
        } =
            true;

        public int CallCount
        {
            get;
            private set;
        }

        public Task<bool> CanChangeStatusAsync(
            OvertimeRequestStatusAuthorizationRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(
                Result);
        }
    }

    private sealed class StubPeriodLockPolicy
        : IAttendancePeriodLockPolicy
    {
        public bool IsLocked
        {
            get;
            set;
        }

        public int CallCount
        {
            get;
            private set;
        }

        public Task<bool> IsLockedAsync(
            DateOnly workDate,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(
                IsLocked);
        }
    }

    private sealed class StubCurrentUserContext
        : ICurrentUserContext
    {
        public AuthenticatedUser? CurrentUser
        {
            get;
            set;
        }

        public bool IsAuthenticated =>
            CurrentUser is not null;
    }

    private sealed class FixedTimeProvider
        : TimeProvider
    {
        private readonly DateTimeOffset
            _utcNow;

        public FixedTimeProvider(
            DateTimeOffset utcNow)
        {
            _utcNow =
                utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public override TimeZoneInfo LocalTimeZone =>
            TimeZoneInfo.Utc;
    }

    private static DateTime Utc(
        int hour)
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
}
