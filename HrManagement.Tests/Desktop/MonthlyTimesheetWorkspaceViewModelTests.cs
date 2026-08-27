using HrManagement.Desktop.Services;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Attendance.Timesheets;

namespace HrManagement.Tests.Desktop;

public sealed class MonthlyTimesheetWorkspaceViewModelTests
{
    [Fact]
    public void Constructor_InitializesCurrentYearAndMonth()
    {
        var queryService =
            new StubMonthlyTimesheetQueryService();

        var timeProvider =
            new FixedTimeProvider(
                new DateTimeOffset(
                    2026,
                    8,
                    25,
                    8,
                    0,
                    0,
                    TimeSpan.Zero));

        var viewModel =
            new MonthlyTimesheetWorkspaceViewModel(
                queryService,
                new StubCloseTimesheetPeriodService(),
                new StubUserConfirmationService(),
                timeProvider);

        Assert.Equal(
            2026,
            viewModel.SelectedYear);

        Assert.Equal(
            8,
            viewModel.SelectedMonth);

        Assert.Contains(
            2026,
            viewModel.YearOptions);

        Assert.Equal(
            12,
            viewModel.MonthOptions.Count);

        Assert.Equal(
            Enumerable.Range(
                1,
                12),
            viewModel.MonthOptions);
    }

    [Fact]
    public async Task LoadAsync_LoadsSelectedPeriod()
    {
        var expected =
            new MonthlyTimesheetReadModel(
                2026,
                7,
                TimesheetPeriodId:
                    null,
                TimesheetPeriodStatus.Open,
                Items:
                    []);

        var queryService =
            new StubMonthlyTimesheetQueryService
            {
                Result =
                    expected
            };

        var viewModel =
            CreateViewModel(
                queryService);

        viewModel.SelectedYear =
            2026;

        viewModel.SelectedMonth =
            7;

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            1,
            queryService.CallCount);

        Assert.Equal(
            2026,
            queryService.LastYear);

        Assert.Equal(
            7,
            queryService.LastMonth);

        Assert.Same(
            expected,
            viewModel.Timesheet);

        Assert.False(
            viewModel.IsLoading);

        Assert.Null(
            viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_WhenQueryFails_ShowsErrorAndClearsTimesheet()
    {
        var queryService =
            new StubMonthlyTimesheetQueryService
            {
                Exception =
                    new InvalidOperationException(
                        "Không thể tải bảng công.")
            };

        var viewModel =
            CreateViewModel(
                queryService);

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            1,
            queryService.CallCount);

        Assert.Null(
            viewModel.Timesheet);

        Assert.False(
            viewModel.IsLoading);

        Assert.Equal(
            "Không thể tải bảng công.",
            viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_WithInvalidMonth_FailsBeforeQuery()
    {
        var queryService =
            new StubMonthlyTimesheetQueryService();

        var viewModel =
            CreateViewModel(
                queryService);

        viewModel.SelectedMonth =
            13;

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            0,
            queryService.CallCount);

        Assert.Null(
            viewModel.Timesheet);

        Assert.Equal(
            "Tháng bảng công phải từ 1 đến 12.",
            viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_MapsRowsAndBuildsOperationalSummary()
    {
        Guid firstEmployeeId =
            Guid.NewGuid();

        Guid secondEmployeeId =
            Guid.NewGuid();

        var readModel =
            new MonthlyTimesheetReadModel(
                2026,
                8,
                TimesheetPeriodId:
                    null,
                TimesheetPeriodStatus.Open,
                Items:
                [
                    new MonthlyTimesheetDayItem(
                    Guid.NewGuid(),
                    firstEmployeeId,
                    new DateOnly(
                        2026,
                        8,
                        24),
                    true,
                    480,
                    AttendanceCalculationStatus.Present,
                    480,
                    5,
                    0,
                    0,
                    "NV001",
                    "Nguyễn Văn An"),

                new MonthlyTimesheetDayItem(
                    Guid.NewGuid(),
                    firstEmployeeId,
                    new DateOnly(
                        2026,
                        8,
                        25),
                    true,
                    480,
                    AttendanceCalculationStatus.NotCalculated,
                    0,
                    0,
                    0,
                    3,
                    "NV001",
                    "Nguyễn Văn An"),

                new MonthlyTimesheetDayItem(
                    Guid.NewGuid(),
                    secondEmployeeId,
                    new DateOnly(
                        2026,
                        8,
                        24),
                    false,
                    0,
                    AttendanceCalculationStatus.NonWorkingDay,
                    0,
                    0,
                    0,
                    0,
                    "NV002",
                    "Trần Thị Bình")
                ]);

        var queryService =
            new StubMonthlyTimesheetQueryService
            {
                Result =
                    readModel
            };

        MonthlyTimesheetWorkspaceViewModel viewModel =
            CreateViewModel(
                queryService);

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            "Đang mở",
            viewModel.PeriodStatusText);

        Assert.Equal(
            "Dữ liệu trực tiếp",
            viewModel.DataSourceText);

        Assert.Equal(
            2,
            viewModel.EmployeeCount);

        Assert.Equal(
            2,
            viewModel.FinalizedDayCount);

        Assert.Equal(
            1,
            viewModel.PendingDayCount);

        Assert.False(
            viewModel.CanAttemptClosePeriod);

        Assert.Equal(
            "Còn 1 ngày chờ xử lý",
            viewModel.ClosingReadinessText);

        Assert.Equal(
            "—",
            viewModel.ClosedAtText);

        Assert.Equal(
            "—",
            viewModel.ClosedByText);

        Assert.Equal(
            1,
            viewModel.CorrectedDayCount);

        Assert.Equal(
            3,
            viewModel.Rows.Count);

        MonthlyTimesheetRowViewModel firstRow =
            viewModel.Rows[0];

        Assert.Equal(
            "NV001",
            firstRow.EmployeeCode);

        Assert.Equal(
            "Nguyễn Văn An",
            firstRow.EmployeeFullName);

        Assert.Equal(
            "24/08/2026",
            firstRow.WorkDateText);

        Assert.Equal(
            "Có mặt",
            firstRow.StatusText);

        MonthlyTimesheetRowViewModel correctedRow =
            Assert.Single(
                viewModel.Rows,
                row =>
                    row.CorrectionRevision == 3);

        Assert.Equal(
            "Chưa tính",
            correctedRow.StatusText);

        Assert.Equal(
            "R3",
            correctedRow.CorrectionText);

        Assert.False(
            correctedRow.IsFinalized);
    }

    [Fact]
    public async Task LoadAsync_WhenPeriodIsClosed_ShowsClosedSnapshotSource()
    {
        var readModel =
            new MonthlyTimesheetReadModel(
                2026,
                8,
                Guid.NewGuid(),
                TimesheetPeriodStatus.Closed,
                Items:
                    [],
                ClosedAtUtc:
                    new DateTime(
                        2026,
                        8,
                        31,
                        12,
                        30,
                        0,
                        DateTimeKind.Utc),
                ClosedByUserId:
                    "user-1",
                ClosedByUsername:
                    "admin");

        var queryService =
            new StubMonthlyTimesheetQueryService
            {
                Result =
                    readModel
            };

        MonthlyTimesheetWorkspaceViewModel viewModel =
            CreateViewModel(
                queryService);

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            "Đã đóng",
            viewModel.PeriodStatusText);

        Assert.Equal(
            "Bản chụp đã đóng",
            viewModel.DataSourceText);

        Assert.False(
            viewModel.CanAttemptClosePeriod);

        Assert.Equal(
            "Kỳ đã đóng",
            viewModel.ClosingReadinessText);

        Assert.Equal(
            "31/08/2026 12:30",
            viewModel.ClosedAtText);

        Assert.Equal(
            "admin",
            viewModel.ClosedByText);
    }

    [Fact]
    public async Task LoadAsync_WhenOpenAndAllRowsAreFinalized_IsReadyToAttemptClose()
    {
        var readModel =
            new MonthlyTimesheetReadModel(
                2026,
                8,
                TimesheetPeriodId:
                    null,
                TimesheetPeriodStatus.Open,
                Items:
                [
                    new MonthlyTimesheetDayItem(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        24),
                    true,
                    480,
                    AttendanceCalculationStatus.Present,
                    480,
                    0,
                    0,
                    0,
                    "NV001",
                    "Nguyễn Văn An")
                ]);

        var queryService =
            new StubMonthlyTimesheetQueryService
            {
                Result =
                    readModel
            };

        MonthlyTimesheetWorkspaceViewModel viewModel =
            CreateViewModel(
                queryService);

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        Assert.True(
            viewModel.CanAttemptClosePeriod);

        Assert.Equal(
            "Sẵn sàng kiểm tra đóng kỳ",
            viewModel.ClosingReadinessText);
    }

    [Fact]
    public async Task ClosePeriodAsync_WhenConfirmationIsRejected_DoesNotClose()
    {
        var queryService =
            new StubMonthlyTimesheetQueryService
            {
                Result =
                    CreateReadyOpenTimesheet()
            };

        var closeService =
            new StubCloseTimesheetPeriodService();

        var confirmationService =
            new StubUserConfirmationService
            {
                Result =
                    false
            };

        MonthlyTimesheetWorkspaceViewModel viewModel =
            CreateViewModel(
                queryService,
                closeService,
                confirmationService);

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        Assert.True(
            viewModel.CanAttemptClosePeriod);

        await viewModel.ClosePeriodCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            1,
            confirmationService.CallCount);

        Assert.Equal(
            0,
            closeService.CallCount);

        Assert.False(
            viewModel.Timesheet!.IsClosed);
    }

    [Fact]
    public async Task ClosePeriodAsync_WhenSuccessful_RefreshesClosedSnapshot()
    {
        MonthlyTimesheetReadModel openTimesheet =
            CreateReadyOpenTimesheet();

        var queryService =
            new StubMonthlyTimesheetQueryService
            {
                Result =
                    openTimesheet
            };

        var closeService =
            new StubCloseTimesheetPeriodService
            {
                Result =
                    new CloseTimesheetPeriodResult(
                        IsSuccessful:
                            true,
                        TimesheetPeriodId:
                            Guid.NewGuid(),
                        SnapshotCount:
                            1,
                        ClosedAtUtc:
                            new DateTime(
                                2026,
                                8,
                                31,
                                12,
                                30,
                                0,
                                DateTimeKind.Utc))
            };

        var confirmationService =
            new StubUserConfirmationService
            {
                Result =
                    true
            };

        MonthlyTimesheetWorkspaceViewModel viewModel =
            CreateViewModel(
                queryService,
                closeService,
                confirmationService);

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        queryService.Result =
            new MonthlyTimesheetReadModel(
                2026,
                8,
                Guid.NewGuid(),
                TimesheetPeriodStatus.Closed,
                openTimesheet.Items,
                ClosedAtUtc:
                    new DateTime(
                        2026,
                        8,
                        31,
                        12,
                        30,
                        0,
                        DateTimeKind.Utc),
                ClosedByUserId:
                    "user-1",
                ClosedByUsername:
                    "admin");

        await viewModel.ClosePeriodCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            1,
            closeService.CallCount);

        Assert.NotNull(
            closeService.LastRequest);

        Assert.Equal(
            2026,
            closeService.LastRequest!.Year);

        Assert.Equal(
            8,
            closeService.LastRequest.Month);

        Assert.Equal(
            2,
            queryService.CallCount);

        Assert.True(
            viewModel.Timesheet!.IsClosed);

        Assert.Equal(
            "Đã đóng",
            viewModel.PeriodStatusText);

        Assert.Equal(
            "Bản chụp đã đóng",
            viewModel.DataSourceText);

        Assert.Equal(
            "admin",
            viewModel.ClosedByText);

        Assert.False(
            viewModel.CanAttemptClosePeriod);
    }

    [Fact]
    public async Task ClosePeriodAsync_WhenServiceRejects_ShowsErrorAndDoesNotRefresh()
    {
        var queryService =
            new StubMonthlyTimesheetQueryService
            {
                Result =
                    CreateReadyOpenTimesheet()
            };

        var closeService =
            new StubCloseTimesheetPeriodService
            {
                Result =
                    new CloseTimesheetPeriodResult(
                        IsSuccessful:
                            false,
                        ErrorMessage:
                            "Kỳ công còn dữ liệu chưa hoàn tất.")
            };

        var confirmationService =
            new StubUserConfirmationService
            {
                Result =
                    true
            };

        MonthlyTimesheetWorkspaceViewModel viewModel =
            CreateViewModel(
                queryService,
                closeService,
                confirmationService);

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        await viewModel.ClosePeriodCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            1,
            closeService.CallCount);

        Assert.Equal(
            1,
            queryService.CallCount);

        Assert.Equal(
            "Kỳ công còn dữ liệu chưa hoàn tất.",
            viewModel.ErrorMessage);

        Assert.False(
            viewModel.Timesheet!.IsClosed);
    }

    [Fact]
    public async Task ClosePeriodCommand_WhenSelectionChangesWithoutRefresh_IsDisabled()
    {
        var queryService =
            new StubMonthlyTimesheetQueryService
            {
                Result =
                    CreateReadyOpenTimesheet()
            };

        MonthlyTimesheetWorkspaceViewModel viewModel =
            CreateViewModel(
                queryService);

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        Assert.True(
            viewModel.ClosePeriodCommand
                .CanExecute(
                    null));

        viewModel.SelectedMonth =
            9;

        Assert.False(
            viewModel.ClosePeriodCommand
                .CanExecute(
                    null));
    }

    private static MonthlyTimesheetReadModel
    CreateReadyOpenTimesheet()
    {
        return new MonthlyTimesheetReadModel(
            2026,
            8,
            TimesheetPeriodId:
                null,
            TimesheetPeriodStatus.Open,
            Items:
            [
                new MonthlyTimesheetDayItem(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    24),
                true,
                480,
                AttendanceCalculationStatus.Present,
                480,
                0,
                0,
                0,
                "NV001",
                "Nguyễn Văn An")
            ]);
    }

    private static MonthlyTimesheetWorkspaceViewModel
    CreateViewModel(
        StubMonthlyTimesheetQueryService? queryService = null,
        StubCloseTimesheetPeriodService? closePeriodService = null,
        StubUserConfirmationService? confirmationService = null)
    {
        return new MonthlyTimesheetWorkspaceViewModel(
            queryService
                ?? new StubMonthlyTimesheetQueryService(),
            closePeriodService
                ?? new StubCloseTimesheetPeriodService(),
            confirmationService
                ?? new StubUserConfirmationService(),
            new FixedTimeProvider(
                new DateTimeOffset(
                    2026,
                    8,
                    25,
                    8,
                    0,
                    0,
                    TimeSpan.Zero)));
    }

    private sealed class StubMonthlyTimesheetQueryService
        : IMonthlyTimesheetQueryService
    {
        public MonthlyTimesheetReadModel Result
        {
            get;
            set;
        } =
            new(
                2026,
                8,
                TimesheetPeriodId:
                    null,
                TimesheetPeriodStatus.Open,
                Items:
                    []);

        public Exception? Exception
        {
            get;
            set;
        }

        public int CallCount
        {
            get;
            private set;
        }

        public int? LastYear
        {
            get;
            private set;
        }

        public int? LastMonth
        {
            get;
            private set;
        }

        public Task<MonthlyTimesheetReadModel> GetAsync(
            int year,
            int month,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            LastYear =
                year;

            LastMonth =
                month;

            if (Exception is not null)
            {
                return Task.FromException<
                    MonthlyTimesheetReadModel>(
                        Exception);
            }

            return Task.FromResult(
                Result);
        }
    }

    private sealed class StubCloseTimesheetPeriodService
    : ICloseTimesheetPeriodService
    {
        public CloseTimesheetPeriodResult Result
        {
            get;
            set;
        } =
            new(
                IsSuccessful:
                    true,
                TimesheetPeriodId:
                    Guid.NewGuid(),
                SnapshotCount:
                    0,
                ClosedAtUtc:
                    new DateTime(
                        2026,
                        8,
                        31,
                        12,
                        0,
                        0,
                        DateTimeKind.Utc));

        public int CallCount
        {
            get;
            private set;
        }

        public CloseTimesheetPeriodRequest? LastRequest
        {
            get;
            private set;
        }

        public Task<CloseTimesheetPeriodResult> CloseAsync(
            CloseTimesheetPeriodRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            LastRequest =
                request;

            return Task.FromResult(
                Result);
        }
    }

    private sealed class StubUserConfirmationService
        : IUserConfirmationService
    {
        public bool Result
        {
            get;
            set;
        }

        public int CallCount
        {
            get;
            private set;
        }

        public string? LastTitle
        {
            get;
            private set;
        }

        public string? LastMessage
        {
            get;
            private set;
        }

        public bool Confirm(
            string title,
            string message)
        {
            CallCount++;

            LastTitle =
                title;

            LastMessage =
                message;

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
}
