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
                    []);

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
    }

    private static MonthlyTimesheetWorkspaceViewModel
        CreateViewModel(
            StubMonthlyTimesheetQueryService? queryService = null)
    {
        return new MonthlyTimesheetWorkspaceViewModel(
            queryService
                ?? new StubMonthlyTimesheetQueryService(),
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
