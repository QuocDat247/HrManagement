using HrManagement.Application.Authentication;
using HrManagement.Application.Employees;
using HrManagement.Application.Payroll.Compensation;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Payroll.Compensation;

namespace HrManagement.Tests.Payroll;

public sealed class EmployeeCompensationServiceTests
{
    [Fact]
    public async Task SetAsync_WhenNoCurrentCompensation_CreatesInitialCompensation()
    {
        TestContext context =
            CreateContext();

        SetEmployeeCompensationResult result =
            await context.Service.SetAsync(
                ValidRequest(
                    context.EmployeeId));

        Assert.True(
            result.IsSuccessful);

        Assert.NotNull(
            result.CompensationId);

        Assert.Null(
            result.PreviousCompensationId);

        EmployeeCompensation persisted =
            Assert.Single(
                context.Persistence.NewCompensations);

        Assert.Equal(
            context.EmployeeId,
            persisted.EmployeeId);

        Assert.Equal(
            context.EmploymentPeriod.Id,
            persisted.EmploymentPeriodId);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                1),
            persisted.EffectiveFrom);

        Assert.Equal(
            25_000_000m,
            persisted.MonthlyBaseSalary);

        Assert.Equal(
            "VND",
            persisted.CurrencyCode);

        Assert.True(
            persisted.IsOpen);

        Assert.Null(
            context.Persistence.ClosedCompensation);

        Assert.Equal(
            "user-1",
            context.Persistence.LastActorUserId);
    }

    [Fact]
    public async Task SetAsync_WhenCurrentCompensationExists_ClosesOldAndCreatesNew()
    {
        TestContext context =
            CreateContext();

        var current =
            new EmployeeCompensation(
                Guid.NewGuid(),
                context.EmployeeId,
                context.EmploymentPeriod.Id,
                new DateOnly(
                    2026,
                    8,
                    1),
                25_000_000m,
                "VND");

        context.ContextSource.Context =
            new EmployeeCompensationContext(
                context.EmploymentPeriod,
                current);

        SetEmployeeCompensationResult result =
            await context.Service.SetAsync(
                new SetEmployeeCompensationRequest(
                    context.EmployeeId,
                    new DateOnly(
                        2026,
                        9,
                        1),
                    28_000_000m,
                    "VND"));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            current.Id,
            result.PreviousCompensationId);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                31),
            current.EffectiveTo);

        Assert.False(
            current.IsOpen);

        Assert.Same(
            current,
            context.Persistence.ClosedCompensation);

        EmployeeCompensation replacement =
            Assert.Single(
                context.Persistence.NewCompensations);

        Assert.Equal(
            new DateOnly(
                2026,
                9,
                1),
            replacement.EffectiveFrom);

        Assert.Equal(
            28_000_000m,
            replacement.MonthlyBaseSalary);

        Assert.True(
            replacement.IsOpen);
    }

    [Fact]
    public async Task SetAsync_WhenNotAuthenticated_FailsBeforeAuthorization()
    {
        TestContext context =
            CreateContext();

        context.CurrentUserContext.CurrentUser =
            null;

        SetEmployeeCompensationResult result =
            await context.Service.SetAsync(
                ValidRequest(
                    context.EmployeeId));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            0,
            context.AuthorizationPolicy.CallCount);

        Assert.Equal(
            0,
            context.EmployeeRepository.GetByIdCallCount);

        Assert.Empty(
            context.Persistence.NewCompensations);
    }

    [Fact]
    public async Task SetAsync_WhenUnauthorized_FailsBeforeEmployeeLookup()
    {
        TestContext context =
            CreateContext();

        context.AuthorizationPolicy.Result =
            false;

        SetEmployeeCompensationResult result =
            await context.Service.SetAsync(
                ValidRequest(
                    context.EmployeeId));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            0,
            context.EmployeeRepository.GetByIdCallCount);

        Assert.Empty(
            context.Persistence.NewCompensations);
    }

    [Fact]
    public async Task SetAsync_WhenEmployeeDoesNotExist_Fails()
    {
        TestContext context =
            CreateContext();

        context.EmployeeRepository.Employee =
            null;

        SetEmployeeCompensationResult result =
            await context.Service.SetAsync(
                ValidRequest(
                    context.EmployeeId));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Không tìm thấy nhân viên.",
            result.ErrorMessage);

        Assert.Empty(
            context.Persistence.NewCompensations);
    }

    [Fact]
    public async Task SetAsync_WhenEffectiveDateIsOutsideEmploymentPeriod_Fails()
    {
        TestContext context =
            CreateContext();

        context.ContextSource.Context =
            null;

        SetEmployeeCompensationResult result =
            await context.Service.SetAsync(
                ValidRequest(
                    context.EmployeeId));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Ngày hiệu lực lương không nằm trong giai đoạn làm việc của nhân viên.",
            result.ErrorMessage);

        Assert.Empty(
            context.Persistence.NewCompensations);
    }

    [Fact]
    public async Task SetAsync_WhenRevisionDateIsNotAfterCurrentStart_FailsWithoutMutation()
    {
        TestContext context =
            CreateContext();

        var current =
            new EmployeeCompensation(
                Guid.NewGuid(),
                context.EmployeeId,
                context.EmploymentPeriod.Id,
                new DateOnly(
                    2026,
                    8,
                    1),
                25_000_000m,
                "VND");

        context.ContextSource.Context =
            new EmployeeCompensationContext(
                context.EmploymentPeriod,
                current);

        SetEmployeeCompensationResult result =
            await context.Service.SetAsync(
                new SetEmployeeCompensationRequest(
                    context.EmployeeId,
                    new DateOnly(
                        2026,
                        8,
                        1),
                    28_000_000m,
                    "VND"));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Ngày hiệu lực lương mới phải sau ngày bắt đầu của cấu hình lương hiện tại.",
            result.ErrorMessage);

        Assert.True(
            current.IsOpen);

        Assert.Null(
            current.EffectiveTo);

        Assert.Empty(
            context.Persistence.NewCompensations);
    }

    [Fact]
    public async Task SetAsync_WhenSalaryIsNegative_FailsBeforeAuthorization()
    {
        TestContext context =
            CreateContext();

        SetEmployeeCompensationResult result =
            await context.Service.SetAsync(
                new SetEmployeeCompensationRequest(
                    context.EmployeeId,
                    new DateOnly(
                        2026,
                        8,
                        1),
                    -1m,
                    "VND"));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Lương cơ bản tháng không được âm.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            context.AuthorizationPolicy.CallCount);
    }

    private static SetEmployeeCompensationRequest
        ValidRequest(
            Guid employeeId)
    {
        return new SetEmployeeCompensationRequest(
            employeeId,
            new DateOnly(
                2026,
                8,
                1),
            25_000_000m,
            "vnd");
    }

    private static TestContext CreateContext()
    {
        Guid employeeId =
            Guid.NewGuid();

        var employmentPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(
                    2026,
                    1,
                    1));

        var employeeRepository =
            new StubEmployeeRepository
            {
                Employee =
                    CreateEmployee(
                        employeeId)
            };

        var contextSource =
            new StubContextSource
            {
                Context =
                    new EmployeeCompensationContext(
                        employmentPeriod,
                        null)
            };

        var persistence =
            new StubPersistence();

        var authorizationPolicy =
            new StubAuthorizationPolicy();

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
            new EmployeeCompensationService(
                employeeRepository,
                contextSource,
                persistence,
                authorizationPolicy,
                currentUserContext);

        return new TestContext(
            service,
            employeeId,
            employmentPeriod,
            employeeRepository,
            contextSource,
            persistence,
            authorizationPolicy,
            currentUserContext);
    }

    private static Employee CreateEmployee(
        Guid employeeId)
    {
        return new Employee(
            employeeId,
            "EMP001",
            "Nguyễn Văn An",
            email:
                null,
            phoneNumber:
                null,
            dateOfBirth:
                null,
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

    private sealed record TestContext(
        EmployeeCompensationService Service,
        Guid EmployeeId,
        EmploymentPeriod EmploymentPeriod,
        StubEmployeeRepository EmployeeRepository,
        StubContextSource ContextSource,
        StubPersistence Persistence,
        StubAuthorizationPolicy AuthorizationPolicy,
        StubCurrentUserContext CurrentUserContext);

    private sealed class StubContextSource
        : IEmployeeCompensationContextSource
    {
        public EmployeeCompensationContext? Context
        {
            get;
            set;
        }

        public Task<EmployeeCompensationContext?> GetAsync(
            Guid employeeId,
            DateOnly effectiveFrom,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Context);
        }
    }

    private sealed class StubPersistence
        : IEmployeeCompensationPersistence
    {
        public EmployeeCompensation? ClosedCompensation
        {
            get;
            private set;
        }

        public List<EmployeeCompensation> NewCompensations
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
            EmployeeCompensation? closedCompensation,
            EmployeeCompensation newCompensation,
            string actorUserId,
            string actorUsername,
            CancellationToken cancellationToken = default)
        {
            ClosedCompensation =
                closedCompensation;

            NewCompensations.Add(
                newCompensation);

            LastActorUserId =
                actorUserId;

            LastActorUsername =
                actorUsername;

            return Task.CompletedTask;
        }
    }

    private sealed class StubAuthorizationPolicy
        : IEmployeeCompensationAuthorizationPolicy
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

        public Task<bool> CanSetAsync(
            EmployeeCompensationAuthorizationRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

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
}
