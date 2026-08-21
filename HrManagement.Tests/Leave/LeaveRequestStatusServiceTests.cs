using HrManagement.Application.Authentication;
using HrManagement.Application.Leave.Requests;
using HrManagement.Domain.Leave.Requests;

namespace HrManagement.Tests.Leave;

public sealed class LeaveRequestStatusServiceTests
{
    [Fact]
    public async Task PendingToApproved_UsesCurrentUserAndSystemTime()
    {
        TestContext test =
            CreateContext();

        LeaveRequest request =
            CreateRequest();

        test.LeaveRequestRepository.Request =
            request;

        ChangeLeaveRequestStatusResult result =
            await test.Service.ChangeStatusAsync(
                new ChangeLeaveRequestStatusRequest(
                    request.Id,
                    LeaveRequestStatus.Approved,
                    "  Đã kiểm tra hồ sơ  "));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            request.Id,
            result.LeaveRequestId);

        Assert.Equal(
            LeaveRequestStatus.Approved,
            result.Status);

        Assert.NotNull(
            result.StatusChangeId);

        LeaveRequestStatusChange saved =
            Assert.IsType<LeaveRequestStatusChange>(
                test.Persistence.Change);

        Assert.Equal(
            request.Id,
            saved.LeaveRequestId);

        Assert.Equal(
            LeaveRequestStatus.Pending,
            saved.FromStatus);

        Assert.Equal(
            LeaveRequestStatus.Approved,
            saved.ToStatus);

        Assert.Equal(
            FixedUtc,
            saved.ChangedAtUtc);

        Assert.Equal(
            "user-001",
            saved.ChangedByUserId);

        Assert.Equal(
            "admin",
            saved.ChangedByUsername);

        Assert.Equal(
            "Đã kiểm tra hồ sơ",
            saved.Note);
    }

    [Fact]
    public async Task PendingToRejected_IsSuccessful()
    {
        TestContext test =
            CreateContext();

        LeaveRequest request =
            CreateRequest();

        test.LeaveRequestRepository.Request =
            request;

        ChangeLeaveRequestStatusResult result =
            await test.Service.ChangeStatusAsync(
                new ChangeLeaveRequestStatusRequest(
                    request.Id,
                    LeaveRequestStatus.Rejected,
                    "Không đủ điều kiện"));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            LeaveRequestStatus.Rejected,
            result.Status);

        Assert.Equal(
            LeaveRequestStatus.Rejected,
            test.Persistence.Change!.ToStatus);
    }

    [Fact]
    public async Task PendingToCancelled_IsSuccessful()
    {
        TestContext test =
            CreateContext();

        LeaveRequest request =
            CreateRequest();

        test.LeaveRequestRepository.Request =
            request;

        ChangeLeaveRequestStatusResult result =
            await test.Service.ChangeStatusAsync(
                new ChangeLeaveRequestStatusRequest(
                    request.Id,
                    LeaveRequestStatus.Cancelled,
                    "Hủy theo yêu cầu"));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            LeaveRequestStatus.Cancelled,
            result.Status);
    }

    [Fact]
    public async Task ApprovedToCancelled_IsSuccessful()
    {
        TestContext test =
            CreateContext();

        LeaveRequest request =
            CreateRequest();

        request.TransitionTo(
            Guid.NewGuid(),
            LeaveRequestStatus.Approved,
            Utc(
                2026,
                8,
                20,
                5,
                0),
            "user-002",
            "manager");

        test.LeaveRequestRepository.Request =
            request;

        ChangeLeaveRequestStatusResult result =
            await test.Service.ChangeStatusAsync(
                new ChangeLeaveRequestStatusRequest(
                    request.Id,
                    LeaveRequestStatus.Cancelled));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            LeaveRequestStatus.Cancelled,
            result.Status);

        Assert.Equal(
            LeaveRequestStatus.Approved,
            test.Persistence.Change!.FromStatus);

        Assert.Equal(
            LeaveRequestStatus.Cancelled,
            test.Persistence.Change.ToStatus);
    }

    [Fact]
    public async Task MissingRequest_ReturnsFailure()
    {
        TestContext test =
            CreateContext();

        ChangeLeaveRequestStatusResult result =
            await test.Service.ChangeStatusAsync(
                new ChangeLeaveRequestStatusRequest(
                    Guid.NewGuid(),
                    LeaveRequestStatus.Approved));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            test.Persistence.Change);
    }

    [Fact]
    public async Task UnauthenticatedUser_ReturnsFailureBeforePersistence()
    {
        TestContext test =
            CreateContext(
                isAuthenticated: false);

        LeaveRequest request =
            CreateRequest();

        test.LeaveRequestRepository.Request =
            request;

        ChangeLeaveRequestStatusResult result =
            await test.Service.ChangeStatusAsync(
                new ChangeLeaveRequestStatusRequest(
                    request.Id,
                    LeaveRequestStatus.Approved));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            test.Persistence.Change);

        Assert.Equal(
            LeaveRequestStatus.Pending,
            request.Status);
    }

    [Fact]
    public async Task PendingTargetStatus_ReturnsFailure()
    {
        TestContext test =
            CreateContext();

        LeaveRequest request =
            CreateRequest();

        test.LeaveRequestRepository.Request =
            request;

        ChangeLeaveRequestStatusResult result =
            await test.Service.ChangeStatusAsync(
                new ChangeLeaveRequestStatusRequest(
                    request.Id,
                    LeaveRequestStatus.Pending));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            test.Persistence.Change);

        Assert.Equal(
            LeaveRequestStatus.Pending,
            request.Status);
    }

    [Fact]
    public async Task ApprovedToRejected_ReturnsFailureWithoutPersistence()
    {
        TestContext test =
            CreateContext();

        LeaveRequest request =
            CreateRequest();

        request.TransitionTo(
            Guid.NewGuid(),
            LeaveRequestStatus.Approved,
            Utc(
                2026,
                8,
                20,
                5,
                0),
            "user-002",
            "manager");

        test.LeaveRequestRepository.Request =
            request;

        ChangeLeaveRequestStatusResult result =
            await test.Service.ChangeStatusAsync(
                new ChangeLeaveRequestStatusRequest(
                    request.Id,
                    LeaveRequestStatus.Rejected));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            test.Persistence.Change);

        Assert.Equal(
            LeaveRequestStatus.Approved,
            request.Status);
    }

    private static readonly DateTime FixedUtc =
        Utc(
            2026,
            8,
            21,
            5,
            30);

    private static TestContext CreateContext(
        bool isAuthenticated = true)
    {
        var leaveRequestRepository =
            new StubLeaveRequestRepository();

        var persistence =
            new StubTransitionPersistence();

        var currentUserContext =
            new StubCurrentUserContext(
                isAuthenticated
                    ? new AuthenticatedUser(
                        "user-001",
                        "admin",
                        "Administrator")
                    : null,
                isAuthenticated);

        var timeProvider =
            new StubTimeProvider(
                new DateTimeOffset(
                    FixedUtc));

        var service =
            new LeaveRequestStatusService(
                leaveRequestRepository,
                persistence,
                currentUserContext,
                timeProvider);

        return new TestContext(
            service,
            leaveRequestRepository,
            persistence);
    }

    private static LeaveRequest CreateRequest()
    {
        return new LeaveRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(
                2026,
                8,
                25),
            new DateOnly(
                2026,
                8,
                26),
            null,
            Utc(
                2026,
                8,
                20,
                4,
                0));
    }

    private static DateTime Utc(
        int year,
        int month,
        int day,
        int hour,
        int minute)
    {
        return new DateTime(
            year,
            month,
            day,
            hour,
            minute,
            0,
            DateTimeKind.Utc);
    }

    private sealed record TestContext(
        LeaveRequestStatusService Service,
        StubLeaveRequestRepository LeaveRequestRepository,
        StubTransitionPersistence Persistence);

    private sealed class StubLeaveRequestRepository
        : ILeaveRequestRepository
    {
        public LeaveRequest? Request
        {
            get;
            set;
        }

        public Task<LeaveRequest?> GetByIdAsync(
            Guid leaveRequestId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Request?.Id ==
                    leaveRequestId
                    ? Request
                    : null);
        }

        public Task<IReadOnlyList<LeaveRequest>>
            GetOverlappingByEmployeeAsync(
                Guid employeeId,
                DateOnly startDate,
                DateOnly endDate,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<LeaveRequest> result =
                [];

            return Task.FromResult(
                result);
        }
    }

    private sealed class StubTransitionPersistence
        : ILeaveRequestStatusTransitionPersistence
    {
        public LeaveRequestStatusChange? Change
        {
            get;
            private set;
        }

        public Task ApplyAsync(
            LeaveRequestStatusChange statusChange,
            CancellationToken cancellationToken = default)
        {
            Change =
                statusChange;

            return Task.CompletedTask;
        }
    }

    private sealed class StubCurrentUserContext
        : ICurrentUserContext
    {
        public StubCurrentUserContext(
            AuthenticatedUser? currentUser,
            bool isAuthenticated)
        {
            CurrentUser =
                currentUser;

            IsAuthenticated =
                isAuthenticated;
        }

        public AuthenticatedUser? CurrentUser
        {
            get;
        }

        public bool IsAuthenticated
        {
            get;
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
