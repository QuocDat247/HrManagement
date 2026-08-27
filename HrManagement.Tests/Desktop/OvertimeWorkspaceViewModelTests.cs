using HrManagement.Application.Workspaces.Overtime;
using HrManagement.Application.Overtime.Requests;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Tests.Desktop;

public sealed class OvertimeWorkspaceViewModelTests
{
    [Fact]
    public void Constructor_InitializesCurrentPeriodAndFilters()
    {
        OvertimeWorkspaceViewModel viewModel =
            CreateViewModel();

        Assert.Equal(
            2026,
            viewModel.SelectedYear);

        Assert.Equal(
            8,
            viewModel.SelectedMonth);

        Assert.Equal(
            12,
            viewModel.MonthOptions.Count);

        Assert.Null(
            viewModel.SelectedEmployeeOption!
                .EmployeeId);

        Assert.Null(
            viewModel.SelectedStatusOption!
                .Status);

        Assert.Equal(
            5,
            viewModel.StatusOptions.Count);
    }

    [Fact]
    public async Task LoadAsync_LoadsFiltersRowsAndSummary()
    {
        Guid employeeId =
            Guid.NewGuid();

        var queryService =
            new StubQueryService
            {
                Employees =
                [
                    new OvertimeEmployeeOption(
                        employeeId,
                        "EMP001",
                        "Nguyễn Văn An")
                ],

                Snapshot =
                    new OvertimeWorkspaceSnapshot(
                    [
                        new OvertimeWorkspaceItem(
                            Guid.NewGuid(),
                            employeeId,
                            "EMP001",
                            "Nguyễn Văn An",
                            new DateOnly(
                                2026,
                                8,
                                27),
                            120,
                            null,
                            OvertimeRequestStatus.Pending,
                            Utc(
                                10),
                            "Triển khai"),

                        new OvertimeWorkspaceItem(
                            Guid.NewGuid(),
                            employeeId,
                            "EMP001",
                            "Nguyễn Văn An",
                            new DateOnly(
                                2026,
                                8,
                                26),
                            120,
                            90,
                            OvertimeRequestStatus.Approved,
                            Utc(
                                9),
                            "Hỗ trợ")
                    ])
            };

        OvertimeWorkspaceViewModel viewModel =
            CreateViewModel(
                queryService);

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            2,
            viewModel.EmployeeOptions.Count);

        Assert.Equal(
            2,
            viewModel.RequestCount);

        Assert.Equal(
            1,
            viewModel.PendingCount);

        Assert.Equal(
            1,
            viewModel.ApprovedCount);

        Assert.Equal(
            90,
            viewModel.TotalApprovedMinutes);

        Assert.Equal(
            2,
            viewModel.Rows.Count);

        Assert.Equal(
            "Chờ duyệt",
            viewModel.Rows[0].StatusText);

        Assert.Equal(
            "—",
            viewModel.Rows[0].ApprovedMinutesText);

        Assert.Null(
            viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_UsesSelectedEmployeeAndStatusFilters()
    {
        Guid employeeId =
            Guid.NewGuid();

        var queryService =
            new StubQueryService
            {
                Employees =
                [
                    new OvertimeEmployeeOption(
                        employeeId,
                        "EMP001",
                        "Nguyễn Văn An")
                ]
            };

        OvertimeWorkspaceViewModel viewModel =
            CreateViewModel(
                queryService);

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        viewModel.SelectedEmployeeOption =
            viewModel.EmployeeOptions.Single(
                option =>
                    option.EmployeeId ==
                    employeeId);

        viewModel.SelectedStatusOption =
            viewModel.StatusOptions.Single(
                option =>
                    option.Status ==
                    OvertimeRequestStatus.Approved);

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        Assert.NotNull(
            queryService.LastQuery);

        Assert.Equal(
            employeeId,
            queryService.LastQuery!
                .EmployeeId);

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            queryService.LastQuery.Status);
    }

    [Fact]
    public async Task SelectedRow_LoadsHistoryLazily()
    {
        Guid requestId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        var queryService =
            new StubQueryService
            {
                Snapshot =
                    new OvertimeWorkspaceSnapshot(
                    [
                        new OvertimeWorkspaceItem(
                            requestId,
                            employeeId,
                            "EMP001",
                            "Nguyễn Văn An",
                            new DateOnly(
                                2026,
                                8,
                                27),
                            120,
                            90,
                            OvertimeRequestStatus.Approved,
                            Utc(
                                10),
                            null)
                    ]),

                History =
                [
                    new OvertimeStatusHistoryItem(
                        Guid.NewGuid(),
                        requestId,
                        OvertimeRequestStatus.Pending,
                        OvertimeRequestStatus.Approved,
                        90,
                        Utc(
                            12),
                        "admin",
                        "Duyệt một phần")
                ]
            };

        OvertimeWorkspaceViewModel viewModel =
            CreateViewModel(
                queryService);

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        viewModel.SelectedRow =
            Assert.Single(
                viewModel.Rows);

        Assert.Equal(
            1,
            queryService.HistoryCallCount);

        OvertimeStatusHistoryRowViewModel history =
            Assert.Single(
                viewModel.HistoryRows);

        Assert.Equal(
            "Chờ duyệt",
            history.PreviousStatusText);

        Assert.Equal(
            "Đã duyệt",
            history.NewStatusText);

        Assert.Equal(
            "27/08/2026 12:00",
            history.ChangedAtText);

        Assert.Equal(
            "Duyệt một phần",
            history.NoteText);
    }

    [Fact]
    public async Task LoadAsync_WhenMonthIsInvalid_FailsBeforeQuery()
    {
        var queryService =
            new StubQueryService();

        OvertimeWorkspaceViewModel viewModel =
            CreateViewModel(
                queryService);

        viewModel.SelectedMonth =
            13;

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            0,
            queryService.QueryCallCount);

        Assert.Equal(
            "Tháng tra cứu tăng ca phải từ 1 đến 12.",
            viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_PopulatesSubmissionEmployeeOptions()
    {
        Guid employeeId =
            Guid.NewGuid();

        var queryService =
            new StubQueryService
            {
                Employees =
                [
                    new OvertimeEmployeeOption(
                    employeeId,
                    "EMP001",
                    "Nguyễn Văn An")
                ]
            };

        OvertimeWorkspaceViewModel viewModel =
            CreateViewModel(
                queryService);

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        OvertimeSubmissionEmployeeOption option =
            Assert.Single(
                viewModel.SubmissionEmployeeOptions);

        Assert.Equal(
            employeeId,
            option.EmployeeId);

        Assert.Equal(
            "EMP001 — Nguyễn Văn An",
            option.DisplayName);

        Assert.Same(
            option,
            viewModel.SelectedSubmissionEmployeeOption);
    }

    [Fact]
    public async Task SubmitAsync_WhenValid_SubmitsAndRefreshesSubmittedPeriod()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid requestId =
            Guid.NewGuid();

        var queryService =
            new StubQueryService
            {
                Employees =
                [
                    new OvertimeEmployeeOption(
                    employeeId,
                    "EMP001",
                    "Nguyễn Văn An")
                ]
            };

        var submitService =
            new StubSubmitService
            {
                Result =
                    new SubmitOvertimeRequestResult(
                        IsSuccessful:
                            true,
                        OvertimeRequestId:
                            requestId,
                        Status:
                            OvertimeRequestStatus.Pending)
            };

        OvertimeWorkspaceViewModel viewModel =
            CreateViewModel(
                queryService,
                submitService);

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        viewModel.SubmissionWorkDate =
            new DateTime(
                2026,
                9,
                2);

        viewModel.SubmissionRequestedMinutesText =
            "90";

        viewModel.SubmissionReason =
            "Hỗ trợ triển khai";

        await viewModel.SubmitCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            1,
            submitService.CallCount);

        Assert.NotNull(
            submitService.LastRequest);

        Assert.Equal(
            employeeId,
            submitService.LastRequest!.EmployeeId);

        Assert.Equal(
            new DateOnly(
                2026,
                9,
                2),
            submitService.LastRequest.WorkDate);

        Assert.Equal(
            90,
            submitService.LastRequest.RequestedMinutes);

        Assert.Equal(
            "Hỗ trợ triển khai",
            submitService.LastRequest.Reason);

        Assert.Equal(
            2026,
            viewModel.SelectedYear);

        Assert.Equal(
            9,
            viewModel.SelectedMonth);

        Assert.Equal(
            2,
            queryService.QueryCallCount);

        Assert.Equal(
            "Đã gửi yêu cầu tăng ca thành công.",
            viewModel.SubmissionSuccessMessage);

        Assert.Null(
            viewModel.SubmissionErrorMessage);
    }

    [Fact]
    public async Task SubmitAsync_WhenServiceRejects_ShowsErrorWithoutRefresh()
    {
        Guid employeeId =
            Guid.NewGuid();

        var queryService =
            new StubQueryService
            {
                Employees =
                [
                    new OvertimeEmployeeOption(
                    employeeId,
                    "EMP001",
                    "Nguyễn Văn An")
                ]
            };

        var submitService =
            new StubSubmitService
            {
                Result =
                    new SubmitOvertimeRequestResult(
                        IsSuccessful:
                            false,
                        ErrorMessage:
                            "Kỳ công của ngày tăng ca đã được đóng. Không thể gửi yêu cầu tăng ca.")
            };

        OvertimeWorkspaceViewModel viewModel =
            CreateViewModel(
                queryService,
                submitService);

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        await viewModel.SubmitCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            1,
            submitService.CallCount);

        Assert.Equal(
            1,
            queryService.QueryCallCount);

        Assert.Equal(
            "Kỳ công của ngày tăng ca đã được đóng. Không thể gửi yêu cầu tăng ca.",
            viewModel.SubmissionErrorMessage);

        Assert.Null(
            viewModel.SubmissionSuccessMessage);
    }

    private sealed class StubSubmitService
    : ISubmitOvertimeRequestService
    {
        public SubmitOvertimeRequestResult Result
        {
            get;
            set;
        } =
            new(
                IsSuccessful:
                    false,
                ErrorMessage:
                    "Chưa cấu hình kết quả kiểm thử.");

        public SubmitOvertimeRequestRequest? LastRequest
        {
            get;
            private set;
        }

        public int CallCount
        {
            get;
            private set;
        }

        public Task<SubmitOvertimeRequestResult> SubmitAsync(
            SubmitOvertimeRequestRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            LastRequest =
                request;

            return Task.FromResult(
                Result);
        }
    }

    private static OvertimeWorkspaceViewModel CreateViewModel(
        StubQueryService? queryService = null,
        StubSubmitService? submitService = null)
    {
        return new OvertimeWorkspaceViewModel(
            queryService
            ?? new StubQueryService(),
            submitService
            ?? new StubSubmitService(),
            new FixedTimeProvider(
                new DateTimeOffset(
                    2026,
                    8,
                    27,
                    8,
                    0,
                    0,
                    TimeSpan.Zero)));
    }

    private sealed class StubQueryService
        : IOvertimeWorkspaceQueryService
    {
        public OvertimeWorkspaceSnapshot Snapshot
        {
            get;
            set;
        } =
            new(
                []);

        public IReadOnlyList<OvertimeEmployeeOption> Employees
        {
            get;
            set;
        } =
            [];

        public IReadOnlyList<OvertimeStatusHistoryItem> History
        {
            get;
            set;
        } =
            [];

        public OvertimeWorkspaceQuery? LastQuery
        {
            get;
            private set;
        }

        public int QueryCallCount
        {
            get;
            private set;
        }

        public int HistoryCallCount
        {
            get;
            private set;
        }

        public Task<OvertimeWorkspaceSnapshot> GetAsync(
            OvertimeWorkspaceQuery query,
            CancellationToken cancellationToken = default)
        {
            QueryCallCount++;

            LastQuery =
                query;

            return Task.FromResult(
                Snapshot);
        }

        public Task<IReadOnlyList<OvertimeEmployeeOption>>
            GetEmployeesAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Employees);
        }

        public Task<IReadOnlyList<OvertimeStatusHistoryItem>>
            GetHistoryAsync(
                Guid overtimeRequestId,
                CancellationToken cancellationToken = default)
        {
            HistoryCallCount++;

            return Task.FromResult(
                History);
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
            27,
            hour,
            0,
            0,
            DateTimeKind.Utc);
    }
}
