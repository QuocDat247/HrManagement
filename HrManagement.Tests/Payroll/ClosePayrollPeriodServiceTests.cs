using HrManagement.Application.Authentication;
using HrManagement.Application.Payroll.Calculations;
using HrManagement.Application.Payroll.Periods;
using HrManagement.Domain.Payroll.Periods;
using HrManagement.Domain.Payroll.Snapshots;

namespace HrManagement.Tests.Payroll;

public sealed class ClosePayrollPeriodServiceTests
{
    [Fact]
    public async Task CloseAsync_WhenPreviewIsFinalizable_PersistsClosedPeriodAndSnapshots()
    {
        TestContext context =
            CreateContext(
                FinalizablePreview());

        ClosePayrollPeriodResult result =
            await context.Service.CloseAsync(
                new ClosePayrollPeriodRequest(
                    2026,
                    8));

        Assert.True(
            result.IsSuccessful);

        Assert.NotNull(
            result.PayrollPeriodId);

        Assert.Equal(
            1,
            result.SnapshotCount);

        Assert.Equal(
            Utc(
                12),
            result.ClosedAtUtc);

        Assert.Equal(
            1,
            context.Persistence.CallCount);

        PayrollPeriod period =
            context.Persistence.Period
            ?? throw new InvalidOperationException();

        Assert.True(
            period.IsClosed);

        Assert.Equal(
            "user-1",
            period.ClosedByUserId);

        PayrollEmployeeSnapshot snapshot =
            Assert.Single(
                context.Persistence.Snapshots);

        Assert.Equal(
            period.Id,
            snapshot.PayrollPeriodId);

        Assert.Equal(
            25_500_000m,
            snapshot.GrossAmount);

        Assert.Equal(
            "user-1",
            context.Persistence.ActorUserId);

        Assert.Equal(
            "admin",
            context.Persistence.ActorUsername);
    }

    [Fact]
    public async Task CloseAsync_WhenPreviewIsNotFinalizable_DoesNotPersist()
    {
        PayrollPreview preview =
            new(
                2026,
                8,
                Guid.NewGuid(),
                [],
                [
                    new PayrollCalculationIssue(
                        PayrollCalculationIssueCode
                            .OvertimeRequiresReview,
                        Guid.NewGuid(),
                        "Có tăng ca cần xem xét.")
                ]);

        TestContext context =
            CreateContext(
                preview);

        ClosePayrollPeriodResult result =
            await context.Service.CloseAsync(
                new ClosePayrollPeriodRequest(
                    2026,
                    8));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            0,
            context.Persistence.CallCount);
    }

    [Fact]
    public async Task CloseAsync_WhenNotAuthenticated_FailsBeforeAuthorizationAndPreview()
    {
        TestContext context =
            CreateContext(
                FinalizablePreview());

        context.CurrentUserContext.CurrentUser =
            null;

        ClosePayrollPeriodResult result =
            await context.Service.CloseAsync(
                new ClosePayrollPeriodRequest(
                    2026,
                    8));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            0,
            context.AuthorizationPolicy.CallCount);

        Assert.Equal(
            0,
            context.PreviewService.CallCount);
    }

    [Fact]
    public async Task CloseAsync_WhenUnauthorized_FailsBeforePreview()
    {
        TestContext context =
            CreateContext(
                FinalizablePreview());

        context.AuthorizationPolicy.Result =
            false;

        ClosePayrollPeriodResult result =
            await context.Service.CloseAsync(
                new ClosePayrollPeriodRequest(
                    2026,
                    8));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            0,
            context.PreviewService.CallCount);

        Assert.Equal(
            0,
            context.Persistence.CallCount);
    }

    [Fact]
    public async Task CloseAsync_WhenPreviewContainsDuplicateEmployee_FailsWithoutPersistence()
    {
        PayrollEmployeePreview employee =
            FinalizableEmployee();

        PayrollPreview preview =
            new(
                2026,
                8,
                Guid.NewGuid(),
                [
                    employee,
                    employee
                ],
                []);

        TestContext context =
            CreateContext(
                preview);

        ClosePayrollPeriodResult result =
            await context.Service.CloseAsync(
                new ClosePayrollPeriodRequest(
                    2026,
                    8));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Bảng lương xem trước chứa trùng nhân viên.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            context.Persistence.CallCount);
    }

    private static TestContext CreateContext(
        PayrollPreview preview)
    {
        var previewService =
            new StubPreviewService(
                preview);

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
            new ClosePayrollPeriodService(
                previewService,
                persistence,
                authorizationPolicy,
                currentUserContext,
                new FixedTimeProvider(
                    new DateTimeOffset(
                        Utc(
                            12))));

        return new TestContext(
            service,
            previewService,
            persistence,
            authorizationPolicy,
            currentUserContext);
    }

    private static PayrollPreview FinalizablePreview()
    {
        return new PayrollPreview(
            2026,
            8,
            Guid.NewGuid(),
            [
                FinalizableEmployee()
            ],
            []);
    }

    private static PayrollEmployeePreview
        FinalizableEmployee()
    {
        return new PayrollEmployeePreview(
            Guid.NewGuid(),
            "EMP001",
            "Nguyễn Văn An",
            "VND",
            25_000_000m,
            120,
            90,
            500_000m,
            25_500_000m,
            [],
            []);
    }

    private sealed record TestContext(
        ClosePayrollPeriodService Service,
        StubPreviewService PreviewService,
        StubPersistence Persistence,
        StubAuthorizationPolicy AuthorizationPolicy,
        StubCurrentUserContext CurrentUserContext);

    private sealed class StubPreviewService
        : IPayrollPreviewService
    {
        private readonly PayrollPreview
            _preview;

        public int CallCount
        {
            get;
            private set;
        }

        public StubPreviewService(
            PayrollPreview preview)
        {
            _preview =
                preview;
        }

        public Task<PayrollPreview> GetAsync(
            int year,
            int month,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(
                _preview);
        }
    }

    private sealed class StubPersistence
        : IClosePayrollPeriodPersistence
    {
        public int CallCount
        {
            get;
            private set;
        }

        public PayrollPeriod? Period
        {
            get;
            private set;
        }

        public IReadOnlyList<PayrollEmployeeSnapshot> Snapshots
        {
            get;
            private set;
        } = [];

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

        public Task PersistAsync(
            PayrollPeriod payrollPeriod,
            IReadOnlyList<PayrollEmployeeSnapshot> snapshots,
            string actorUserId,
            string actorUsername,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            Period =
                payrollPeriod;

            Snapshots =
                snapshots;

            ActorUserId =
                actorUserId;

            ActorUsername =
                actorUsername;

            return Task.CompletedTask;
        }
    }

    private sealed class StubAuthorizationPolicy
        : IPayrollPeriodClosingAuthorizationPolicy
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

        public Task<bool> CanCloseAsync(
            PayrollPeriodClosingAuthorizationRequest request,
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
            9,
            1,
            hour,
            0,
            0,
            DateTimeKind.Utc);
    }
}
