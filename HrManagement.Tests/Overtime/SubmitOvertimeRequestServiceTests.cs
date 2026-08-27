using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Application.Authentication;
using HrManagement.Application.Employees;
using HrManagement.Application.Overtime.Requests;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Tests.Overtime;

public sealed class SubmitOvertimeRequestServiceTests
{
    [Fact]
    public async Task SubmitAsync_WhenValid_PersistsPendingRequest()
    {
        Guid employeeId =
            Guid.NewGuid();

        EmploymentPeriod employmentPeriod =
            new(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(
                    2026,
                    8,
                    1));

        var persistence =
            new StubPersistence();

        TestContext context =
            CreateContext(
                employeeId,
                employmentPeriod,
                persistence:
                    persistence);

        SubmitOvertimeRequestResult result =
            await context.Service.SubmitAsync(
                new SubmitOvertimeRequestRequest(
                    employeeId,
                    new DateOnly(
                        2026,
                        8,
                        27),
                    120,
                    "Hoàn thành bản phát hành"));

        Assert.True(
            result.IsSuccessful);

        Assert.NotNull(
            result.OvertimeRequestId);

        Assert.Equal(
            OvertimeRequestStatus.Pending,
            result.Status);

        OvertimeRequest persisted =
            Assert.Single(
                persistence.Submitted);

        Assert.Equal(
            employeeId,
            persisted.EmployeeId);

        Assert.Equal(
            employmentPeriod.Id,
            persisted.EmploymentPeriodId);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                27),
            persisted.WorkDate);

        Assert.Equal(
            120,
            persisted.RequestedMinutes);

        Assert.Equal(
            "user-1",
            persistence.LastActorUserId);

        Assert.Equal(
            "admin",
            persistence.LastActorUsername);

        Assert.Equal(
            UtcNow().UtcDateTime,
            persisted.SubmittedAtUtc);
    }

    [Fact]
    public async Task SubmitAsync_WhenNotAuthenticated_FailsBeforeAuthorization()
    {
        TestContext context = CreateContext();

        context.CurrentUserContext.CurrentUser = null;

        SubmitOvertimeRequestResult result = await context.Service.SubmitAsync(
            ValidRequest(context.EmployeeId));

        Assert.False(result.IsSuccessful);
        Assert.Equal("Không thể gửi yêu cầu tăng ca khi chưa có người dùng đăng nhập.", result.ErrorMessage);
        Assert.Equal(0, context.AuthorizationPolicy.CallCount);
        Assert.Empty(context.Persistence.Submitted);
    }

    [Fact]
    public async Task SubmitAsync_WhenNotAuthorized_FailsBeforeEmployeeLookup()
    {
        TestContext context =
            CreateContext();

        context.AuthorizationPolicy.Result =
            false;

        SubmitOvertimeRequestResult result =
            await context.Service.SubmitAsync(
                ValidRequest(
                    context.EmployeeId));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Bạn không có quyền gửi yêu cầu tăng ca.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            context.EmployeeRepository.GetByIdCallCount);

        Assert.Empty(
            context.Persistence.Submitted);
    }

    [Fact]
    public async Task SubmitAsync_WhenEmployeeDoesNotExist_Fails()
    {
        TestContext context =
            CreateContext();

        context.EmployeeRepository.Employee =
            null;

        SubmitOvertimeRequestResult result =
            await context.Service.SubmitAsync(
                ValidRequest(
                    context.EmployeeId));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Không tìm thấy nhân viên.",
            result.ErrorMessage);

        Assert.Empty(
            context.Persistence.Submitted);
    }

    [Fact]
    public async Task SubmitAsync_WhenEmploymentPeriodDoesNotCoverDate_Fails()
    {
        TestContext context =
            CreateContext();

        context.ContextSource.EmploymentPeriod =
            null;

        SubmitOvertimeRequestResult result =
            await context.Service.SubmitAsync(
                ValidRequest(
                    context.EmployeeId));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Ngày tăng ca không nằm trong giai đoạn làm việc của nhân viên.",
            result.ErrorMessage);

        Assert.Empty(
            context.Persistence.Submitted);
    }

    [Fact]
    public async Task SubmitAsync_WhenPeriodIsClosed_FailsBeforePersistence()
    {
        TestContext context =
            CreateContext();

        context.PeriodLockPolicy.IsLocked =
            true;

        SubmitOvertimeRequestResult result =
            await context.Service.SubmitAsync(
                ValidRequest(
                    context.EmployeeId));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Kỳ công của ngày tăng ca đã được đóng. Không thể gửi yêu cầu tăng ca.",
            result.ErrorMessage);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                27),
            context.PeriodLockPolicy.LastWorkDate);

        Assert.Empty(
            context.Persistence.Submitted);
    }

    private static SubmitOvertimeRequestRequest ValidRequest(
        Guid employeeId)
    {
        return new SubmitOvertimeRequestRequest(
            employeeId,
            new DateOnly(
                2026,
                8,
                27),
            120,
            "Kiểm thử tăng ca");
    }

    private static TestContext CreateContext(
        Guid? employeeId = null,
        EmploymentPeriod? employmentPeriod = null,
        StubPersistence? persistence = null,
        AuthenticatedUser? currentUser =
            default)
    {
        Guid resolvedEmployeeId =
            employeeId
            ?? Guid.NewGuid();

        EmploymentPeriod resolvedPeriod =
            employmentPeriod
            ?? new EmploymentPeriod(
                Guid.NewGuid(),
                resolvedEmployeeId,
                new DateOnly(
                    2026,
                    8,
                    1));

        var employeeRepository =
            new StubEmployeeRepository
            {
                Employee =
                    CreateEmployee(
                        resolvedEmployeeId)
            };

        var contextSource =
            new StubContextSource
            {
                EmploymentPeriod =
                    resolvedPeriod
            };

        var periodLockPolicy =
            new StubPeriodLockPolicy();

        var authorizationPolicy =
            new StubAuthorizationPolicy();

        StubPersistence resolvedPersistence =
            persistence
            ?? new StubPersistence();

        var currentUserContext =
            new StubCurrentUserContext
            {
                CurrentUser =
                    currentUser
                    ?? new AuthenticatedUser(
                        "user-1",
                        "admin",
                        "Administrator")
            };

        var service =
            new SubmitOvertimeRequestService(
                employeeRepository,
                contextSource,
                periodLockPolicy,
                authorizationPolicy,
                resolvedPersistence,
                currentUserContext,
                new FixedTimeProvider(
                    UtcNow()));

        return new TestContext(
            service,
            resolvedEmployeeId,
            employeeRepository,
            contextSource,
            periodLockPolicy,
            authorizationPolicy,
            resolvedPersistence,
            currentUserContext);
    }

    private static Employee CreateEmployee(
        Guid employeeId)
    {
        return new Employee(
            employeeId,
            "EMP001",
            "Nguyễn Văn An",
            email: null,
            phoneNumber: null,
            dateOfBirth: null,
            hireDate:
                new DateOnly(
                    2026,
                    1,
                    1),
            department:
                "Phát triển",
            position:
                "Lập trình viên",
            status:
                EmployeeStatus.Active);
    }

    private static DateTimeOffset UtcNow()
    {
        return new DateTimeOffset(
            2026,
            8,
            27,
            13,
            0,
            0,
            TimeSpan.Zero);
    }

    private sealed record TestContext(
        SubmitOvertimeRequestService Service,
        Guid EmployeeId,
        StubEmployeeRepository EmployeeRepository,
        StubContextSource ContextSource,
        StubPeriodLockPolicy PeriodLockPolicy,
        StubAuthorizationPolicy AuthorizationPolicy,
        StubPersistence Persistence,
        StubCurrentUserContext CurrentUserContext);

    private sealed class StubContextSource
        : IOvertimeRequestSubmissionContextSource
    {
        public EmploymentPeriod? EmploymentPeriod
        {
            get;
            set;
        }

        public Task<EmploymentPeriod?> GetEmploymentPeriodAsync(
            Guid employeeId,
            DateOnly workDate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                EmploymentPeriod);
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

        public DateOnly? LastWorkDate
        {
            get;
            private set;
        }

        public Task<bool> IsLockedAsync(
            DateOnly workDate,
            CancellationToken cancellationToken = default)
        {
            LastWorkDate =
                workDate;

            return Task.FromResult(
                IsLocked);
        }
    }

    private sealed class StubAuthorizationPolicy
        : IOvertimeRequestSubmissionAuthorizationPolicy
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

        public Task<bool> CanSubmitAsync(
            OvertimeRequestSubmissionAuthorizationRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(
                Result);
        }
    }

    private sealed class StubPersistence
        : IOvertimeRequestSubmissionPersistence
    {
        public List<OvertimeRequest> Submitted
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

        public Task SubmitAsync(
            OvertimeRequest overtimeRequest,
            string actorUserId,
            string actorUsername,
            CancellationToken cancellationToken = default)
        {
            Submitted.Add(
                overtimeRequest);

            LastActorUserId =
                actorUserId;

            LastActorUsername =
                actorUsername;

            return Task.CompletedTask;
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

    private sealed class StubEmployeeRepository
        : IEmployeeRepository
    {
        public Employee? Employee
        {
            get;
            set;
        }

        public int GetByIdCallCount
        {
            get;
            private set;
        }

        public Task<Employee?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;

            return Task.FromResult(
                Employee);
        }

        public Task<IReadOnlyList<Employee>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Employee?> GetByEmployeeCodeAsync(
            string employeeCode,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(
            Employee employee,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(
            Employee employee,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
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
}
