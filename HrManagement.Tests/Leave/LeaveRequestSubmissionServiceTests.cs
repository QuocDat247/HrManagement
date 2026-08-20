using HrManagement.Application.Employees;
using HrManagement.Application.Employees.EmploymentHistories;
using HrManagement.Application.Leave.Requests;
using HrManagement.Application.Leave.Types;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Leave.Requests;
using HrManagement.Domain.Leave.Types;

namespace HrManagement.Tests.Leave;

public sealed class LeaveRequestSubmissionServiceTests
{
    [Fact]
    public async Task ValidRequestInOpenPeriod_SubmitsPendingRequest()
    {
        TestContext test =
            CreateContext();

        Employee employee =
            CreateEmployee();

        EmploymentPeriod period =
            CreatePeriod(
                employee.Id);

        LeaveType leaveType =
            CreateLeaveType();

        test.EmployeeRepository.Employee =
            employee;

        test.EmploymentHistoryRepository.History =
            new EmploymentHistory(
                employee.Id,
                [period]);

        test.LeaveTypeRepository.LeaveType =
            leaveType;

        SubmitLeaveRequestResult result =
            await test.Service.SubmitAsync(
                new SubmitLeaveRequestRequest(
                    employee.Id,
                    leaveType.Id,
                    new DateOnly(
                        2026,
                        8,
                        20),
                    new DateOnly(
                        2026,
                        8,
                        22),
                    "  Việc gia đình  "));

        Assert.True(
            result.IsSuccessful);

        Assert.NotNull(
            result.LeaveRequestId);

        Assert.Equal(
            LeaveRequestStatus.Pending,
            result.Status);

        LeaveRequest saved =
            Assert.IsType<LeaveRequest>(
                test.Persistence.Request);

        Assert.Equal(
            employee.Id,
            saved.EmployeeId);

        Assert.Equal(
            period.Id,
            saved.EmploymentPeriodId);

        Assert.Equal(
            leaveType.Id,
            saved.LeaveTypeId);

        Assert.Equal(
            "Việc gia đình",
            saved.Reason);

        Assert.Equal(
            FixedUtc,
            saved.SubmittedAtUtc);
    }

    [Fact]
    public async Task HistoricalClosedPeriod_CanBeResolvedByRequestedDates()
    {
        TestContext test =
            CreateContext();

        Employee employee =
            CreateEmployee(
                EmployeeStatus.Inactive);

        EmploymentPeriod historicalPeriod =
            CreatePeriod(
                employee.Id,
                new DateOnly(
                    2026,
                    6,
                    30));

        test.EmployeeRepository.Employee =
            employee;

        test.EmploymentHistoryRepository.History =
            new EmploymentHistory(
                employee.Id,
                [historicalPeriod]);

        test.LeaveTypeRepository.LeaveType =
            CreateLeaveType();

        SubmitLeaveRequestResult result =
            await test.Service.SubmitAsync(
                new SubmitLeaveRequestRequest(
                    employee.Id,
                    test.LeaveTypeRepository.LeaveType.Id,
                    new DateOnly(
                        2026,
                        6,
                        20),
                    new DateOnly(
                        2026,
                        6,
                        21),
                    null));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            historicalPeriod.Id,
            test.Persistence.Request!
                .EmploymentPeriodId);
    }

    [Fact]
    public async Task MissingEmployee_ReturnsFailure()
    {
        TestContext test =
            CreateContext();

        SubmitLeaveRequestResult result =
            await test.Service.SubmitAsync(
                ValidRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid()));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            test.Persistence.Request);
    }

    [Fact]
    public async Task MissingLeaveType_ReturnsFailure()
    {
        TestContext test =
            CreateContext();

        Employee employee =
            CreateEmployee();

        test.EmployeeRepository.Employee =
            employee;

        SubmitLeaveRequestResult result =
            await test.Service.SubmitAsync(
                ValidRequest(
                    employee.Id,
                    Guid.NewGuid()));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            test.Persistence.Request);
    }

    [Fact]
    public async Task InactiveLeaveType_ReturnsFailure()
    {
        TestContext test =
            CreateContext();

        Employee employee =
            CreateEmployee();

        EmploymentPeriod period =
            CreatePeriod(
                employee.Id);

        LeaveType inactiveType =
            new(
                Guid.NewGuid(),
                "OLD",
                "Loại nghỉ cũ",
                isPaid: true,
                isActive: false);

        test.EmployeeRepository.Employee =
            employee;

        test.EmploymentHistoryRepository.History =
            new EmploymentHistory(
                employee.Id,
                [period]);

        test.LeaveTypeRepository.LeaveType =
            inactiveType;

        SubmitLeaveRequestResult result =
            await test.Service.SubmitAsync(
                ValidRequest(
                    employee.Id,
                    inactiveType.Id));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            test.Persistence.Request);
    }

    [Fact]
    public async Task RequestOutsideEmploymentPeriod_ReturnsFailure()
    {
        TestContext test =
            CreateContext();

        Employee employee =
            CreateEmployee();

        EmploymentPeriod period =
            new(
                Guid.NewGuid(),
                employee.Id,
                new DateOnly(
                    2026,
                    1,
                    1),
                new DateOnly(
                    2026,
                    7,
                    31));

        LeaveType leaveType =
            CreateLeaveType();

        test.EmployeeRepository.Employee =
            employee;

        test.EmploymentHistoryRepository.History =
            new EmploymentHistory(
                employee.Id,
                [period]);

        test.LeaveTypeRepository.LeaveType =
            leaveType;

        SubmitLeaveRequestResult result =
            await test.Service.SubmitAsync(
                ValidRequest(
                    employee.Id,
                    leaveType.Id));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            test.Persistence.Request);
    }

    [Fact]
    public async Task RequestSpanningEmploymentGap_ReturnsFailure()
    {
        TestContext test =
            CreateContext();

        Employee employee =
            CreateEmployee();

        EmploymentPeriod first =
            new(
                Guid.NewGuid(),
                employee.Id,
                new DateOnly(
                    2026,
                    1,
                    1),
                new DateOnly(
                    2026,
                    3,
                    31));

        EmploymentPeriod second =
            new(
                Guid.NewGuid(),
                employee.Id,
                new DateOnly(
                    2026,
                    4,
                    10));

        LeaveType leaveType =
            CreateLeaveType();

        test.EmployeeRepository.Employee =
            employee;

        test.EmploymentHistoryRepository.History =
            new EmploymentHistory(
                employee.Id,
                [
                    first,
                    second
                ]);

        test.LeaveTypeRepository.LeaveType =
            leaveType;

        SubmitLeaveRequestResult result =
            await test.Service.SubmitAsync(
                new SubmitLeaveRequestRequest(
                    employee.Id,
                    leaveType.Id,
                    new DateOnly(
                        2026,
                        3,
                        30),
                    new DateOnly(
                        2026,
                        4,
                        11),
                    null));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            test.Persistence.Request);
    }

    [Fact]
    public async Task OverlappingRequest_ReturnsFailure()
    {
        TestContext test =
            CreateContext();

        Employee employee =
            CreateEmployee();

        EmploymentPeriod period =
            CreatePeriod(
                employee.Id);

        LeaveType leaveType =
            CreateLeaveType();

        test.EmployeeRepository.Employee =
            employee;

        test.EmploymentHistoryRepository.History =
            new EmploymentHistory(
                employee.Id,
                [period]);

        test.LeaveTypeRepository.LeaveType =
            leaveType;

        test.LeaveRequestRepository.Requests =
        [
            new LeaveRequest(
                Guid.NewGuid(),
                employee.Id,
                period.Id,
                leaveType.Id,
                new DateOnly(
                    2026,
                    8,
                    21),
                new DateOnly(
                    2026,
                    8,
                    23),
                null,
                FixedUtc)
        ];

        SubmitLeaveRequestResult result =
            await test.Service.SubmitAsync(
                ValidRequest(
                    employee.Id,
                    leaveType.Id));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            test.Persistence.Request);
    }

    [Fact]
    public async Task InvalidDateRange_ReturnsFailureBeforePersistence()
    {
        TestContext test =
            CreateContext();

        SubmitLeaveRequestResult result =
            await test.Service.SubmitAsync(
                new SubmitLeaveRequestRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        22),
                    new DateOnly(
                        2026,
                        8,
                        20),
                    null));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            test.Persistence.Request);
    }

    private static readonly DateTime FixedUtc =
        new(
            2026,
            8,
            20,
            4,
            0,
            0,
            DateTimeKind.Utc);

    private static TestContext CreateContext()
    {
        var employeeRepository =
            new StubEmployeeRepository();

        var employmentHistoryRepository =
            new StubEmploymentHistoryRepository();

        var leaveTypeRepository =
            new StubLeaveTypeRepository();

        var leaveRequestRepository =
            new StubLeaveRequestRepository();

        var persistence =
            new StubLeaveRequestSubmissionPersistence();

        var timeProvider =
            new StubTimeProvider(
                new DateTimeOffset(
                    FixedUtc));

        var service =
            new LeaveRequestSubmissionService(
                employeeRepository,
                employmentHistoryRepository,
                leaveTypeRepository,
                leaveRequestRepository,
                persistence,
                timeProvider);

        return new TestContext(
            service,
            employeeRepository,
            employmentHistoryRepository,
            leaveTypeRepository,
            leaveRequestRepository,
            persistence);
    }

    private static SubmitLeaveRequestRequest ValidRequest(
        Guid employeeId,
        Guid leaveTypeId)
    {
        return new SubmitLeaveRequestRequest(
            employeeId,
            leaveTypeId,
            new DateOnly(
                2026,
                8,
                20),
            new DateOnly(
                2026,
                8,
                22),
            null);
    }

    private static Employee CreateEmployee(
        EmployeeStatus status = EmployeeStatus.Active)
    {
        Guid id =
            Guid.NewGuid();

        return new Employee(
            id,
            $"EMP{id:N}"[..20],
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
            status);
    }

    private static EmploymentPeriod CreatePeriod(
        Guid employeeId,
        DateOnly? endDate = null)
    {
        return new EmploymentPeriod(
            Guid.NewGuid(),
            employeeId,
            new DateOnly(
                2026,
                1,
                1),
            endDate);
    }

    private static LeaveType CreateLeaveType()
    {
        return new LeaveType(
            Guid.NewGuid(),
            "ANNUAL",
            "Nghỉ phép năm",
            isPaid: true);
    }

    private sealed record TestContext(
        LeaveRequestSubmissionService Service,
        StubEmployeeRepository EmployeeRepository,
        StubEmploymentHistoryRepository EmploymentHistoryRepository,
        StubLeaveTypeRepository LeaveTypeRepository,
        StubLeaveRequestRepository LeaveRequestRepository,
        StubLeaveRequestSubmissionPersistence Persistence);

    private sealed class StubEmployeeRepository
        : IEmployeeRepository
    {
        public Employee? Employee
        {
            get;
            set;
        }

        public Task<IReadOnlyList<Employee>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Employee> result =
                Employee is null
                    ? []
                    : [Employee];

            return Task.FromResult(
                result);
        }

        public Task<Employee?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Employee?.Id == id
                    ? Employee
                    : null);
        }

        public Task<Employee?> GetByEmployeeCodeAsync(
            string employeeCode,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Employee?>(
                null);
        }

        public Task AddAsync(
            Employee employee,
            CancellationToken cancellationToken = default)
        {
            Employee =
                employee;

            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            Employee employee,
            CancellationToken cancellationToken = default)
        {
            Employee =
                employee;

            return Task.CompletedTask;
        }
    }

    private sealed class StubEmploymentHistoryRepository
        : IEmploymentHistoryRepository
    {
        public EmploymentHistory? History
        {
            get;
            set;
        }

        public Task<EmploymentHistory> GetByEmployeeIdAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                History ??
                new EmploymentHistory(
                    employeeId,
                    []));
        }

        public Task AddPeriodAsync(
            EmploymentPeriod period,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UpdatePeriodAsync(
            EmploymentPeriod period,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubLeaveTypeRepository
        : ILeaveTypeRepository
    {
        public LeaveType? LeaveType
        {
            get;
            set;
        }

        public Task<LeaveType?> GetByIdAsync(
            Guid leaveTypeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                LeaveType?.Id == leaveTypeId
                    ? LeaveType
                    : null);
        }
    }

    private sealed class StubLeaveRequestRepository
        : ILeaveRequestRepository
    {
        public IReadOnlyList<LeaveRequest> Requests
        {
            get;
            set;
        } = [];

        public Task<LeaveRequest?> GetByIdAsync(
            Guid leaveRequestId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Requests.FirstOrDefault(
                    request =>
                        request.Id ==
                        leaveRequestId));
        }

        public Task<IReadOnlyList<LeaveRequest>>
            GetOverlappingByEmployeeAsync(
                Guid employeeId,
                DateOnly startDate,
                DateOnly endDate,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<LeaveRequest> result =
                Requests
                    .Where(
                        request =>
                            request.EmployeeId ==
                                employeeId
                            && request.StartDate <=
                                endDate
                            && startDate <=
                                request.EndDate)
                    .ToList();

            return Task.FromResult(
                result);
        }
    }

    private sealed class StubLeaveRequestSubmissionPersistence
        : ILeaveRequestSubmissionPersistence
    {
        public LeaveRequest? Request
        {
            get;
            private set;
        }

        public Task SubmitAsync(
            LeaveRequest leaveRequest,
            CancellationToken cancellationToken = default)
        {
            Request =
                leaveRequest;

            return Task.CompletedTask;
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
