using HrManagement.Application.Payroll.Calculations;
using HrManagement.Application.Payroll.Periods;
using HrManagement.Desktop.Services;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Tests.Payroll;

public sealed class PayrollWorkspaceViewModelTests
{
    [Fact]
    public async Task LoadAsync_WhenClosedPayrollExists_UsesImmutableClosedDataWithoutPreview()
    {
        TestContext context =
            CreateContext();

        context.ClosedQuery.Result =
            ClosedPayroll();

        await context.ViewModel.LoadCommand.ExecuteAsync(
            null);

        Assert.True(
            context.ViewModel.IsClosed);

        Assert.False(
            context.ViewModel.IsPreview);

        Assert.Equal(
            0,
            context.PreviewService.CallCount);

        Assert.Equal(
            1,
            context.ViewModel.EmployeeCount);

        PayrollWorkspaceRowViewModel row =
            Assert.Single(
                context.ViewModel.Rows);

        Assert.Equal(
            "Đã khóa",
            row.StatusText);

        Assert.Equal(
            25_000_000m,
            row.GrossAmount);
    }

    [Fact]
    public async Task LoadAsync_WhenNoClosedPayroll_LoadsPreview()
    {
        TestContext context =
            CreateContext();

        context.PreviewService.Result =
            FinalizablePreview();

        await context.ViewModel.LoadCommand.ExecuteAsync(
            null);

        Assert.True(
            context.ViewModel.IsPreview);

        Assert.False(
            context.ViewModel.IsClosed);

        Assert.True(
            context.ViewModel.PreviewIsFinalizable);

        Assert.True(
            context.ViewModel.CanClose);

        Assert.Equal(
            1,
            context.PreviewService.CallCount);
    }

    [Fact]
    public async Task LoadAsync_WhenPreviewHasIssues_DisablesClosing()
    {
        TestContext context =
            CreateContext();

        context.PreviewService.Result =
            new PayrollPreview(
                2026,
                8,
                Guid.NewGuid(),
                [],
                [
                    new PayrollCalculationIssue(
                        PayrollCalculationIssueCode
                            .MissingCompensation,
                        Guid.NewGuid(),
                        "Thiếu cấu hình lương.")
                ]);

        await context.ViewModel.LoadCommand.ExecuteAsync(
            null);

        Assert.False(
            context.ViewModel.PreviewIsFinalizable);

        Assert.False(
            context.ViewModel.CanClose);

        Assert.Contains(
            "Thiếu cấu hình lương.",
            context.ViewModel.IssueMessages);
    }

    [Fact]
    public async Task CloseAsync_WhenConfirmed_ClosesAndReloadsImmutableSnapshot()
    {
        TestContext context =
            CreateContext();

        context.PreviewService.Result =
            FinalizablePreview();

        await context.ViewModel.LoadCommand.ExecuteAsync(
            null);

        context.ConfirmationService.Result =
            true;

        context.CloseService.Result =
            new ClosePayrollPeriodResult(
                true,
                Guid.NewGuid(),
                1,
                Utc(
                    12));

        context.ClosedQuery.Result =
            ClosedPayroll();

        await context.ViewModel
            .ClosePayrollCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            1,
            context.CloseService.CallCount);

        Assert.True(
            context.ViewModel.IsClosed);

        Assert.False(
            context.ViewModel.CanClose);
    }

    [Fact]
    public async Task CloseAsync_WhenUserDeclines_DoesNotCallCloseService()
    {
        TestContext context =
            CreateContext();

        context.PreviewService.Result =
            FinalizablePreview();

        await context.ViewModel.LoadCommand.ExecuteAsync(
            null);

        context.ConfirmationService.Result =
            false;

        await context.ViewModel
            .ClosePayrollCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            0,
            context.CloseService.CallCount);

        Assert.True(
            context.ViewModel.IsPreview);
    }

    private static TestContext CreateContext()
    {
        var previewService =
            new StubPreviewService();

        var closedQuery =
            new StubClosedPayrollQuery();

        var closeService =
            new StubCloseService();

        var confirmationService =
            new StubConfirmationService();

        var viewModel =
            new PayrollWorkspaceViewModel(
                previewService,
                closedQuery,
                closeService,
                confirmationService,
                new FixedTimeProvider(
                    new DateTimeOffset(
                        Utc(
                            10))));

        viewModel.SelectedYear =
            2026;

        viewModel.SelectedMonth =
            8;

        return new TestContext(
            viewModel,
            previewService,
            closedQuery,
            closeService,
            confirmationService);
    }

    private static PayrollPreview FinalizablePreview()
    {
        return new PayrollPreview(
            2026,
            8,
            Guid.NewGuid(),
            [
                new PayrollEmployeePreview(
                    Guid.NewGuid(),
                    "EMP001",
                    "Nguyễn Văn An",
                    "VND",
                    25_000_000m,
                    0,
                    0,
                    0m,
                    25_000_000m,
                    [],
                    [])
            ],
            []);
    }

    private static ClosedPayrollReadModel ClosedPayroll()
    {
        return new ClosedPayrollReadModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            2026,
            8,
            Utc(
                12),
            "user-1",
            "admin",
            [
                new ClosedPayrollEmployeeItem(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "EMP001",
                    "Nguyễn Văn An",
                    "VND",
                    25_000_000m,
                    0,
                    0,
                    0m,
                    25_000_000m)
            ],
            [
                new ClosedPayrollCurrencySummary(
                    "VND",
                    1,
                    25_000_000m,
                    0m,
                    25_000_000m)
            ]);
    }

    private sealed record TestContext(
        PayrollWorkspaceViewModel ViewModel,
        StubPreviewService PreviewService,
        StubClosedPayrollQuery ClosedQuery,
        StubCloseService CloseService,
        StubConfirmationService ConfirmationService);

    private sealed class StubPreviewService
        : IPayrollPreviewService
    {
        public PayrollPreview Result
        {
            get;
            set;
        } =
            FinalizablePreview();

        public int CallCount
        {
            get;
            private set;
        }

        public Task<PayrollPreview> GetAsync(
            int year,
            int month,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(
                Result);
        }
    }

    private sealed class StubClosedPayrollQuery
        : IClosedPayrollQueryService
    {
        public ClosedPayrollReadModel? Result
        {
            get;
            set;
        }

        public int CallCount
        {
            get;
            private set;
        }

        public Task<ClosedPayrollReadModel?> GetAsync(
            int year,
            int month,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(
                Result);
        }
    }

    private sealed class StubCloseService
        : IClosePayrollPeriodService
    {
        public ClosePayrollPeriodResult Result
        {
            get;
            set;
        } =
            new(
                false,
                ErrorMessage:
                    "Chưa cấu hình.");

        public int CallCount
        {
            get;
            private set;
        }

        public Task<ClosePayrollPeriodResult> CloseAsync(
            ClosePayrollPeriodRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(
                Result);
        }
    }

    private sealed class StubConfirmationService
        : IUserConfirmationService
    {
        public bool Result
        {
            get;
            set;
        }

        public bool Confirm(
            string title,
            string message)
        {
            return Result;
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

    private static DateTime Utc(
        int hour)
    {
        return new DateTime(
            2026,
            8,
            31,
            hour,
            0,
            0,
            DateTimeKind.Utc);
    }
}
