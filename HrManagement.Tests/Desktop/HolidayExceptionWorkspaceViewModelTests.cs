using HrManagement.Application.Attendance.Calendars;
using HrManagement.Application.Attendance.Schedules.Overrides;
using HrManagement.Application.Workspaces.HolidayExceptions;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Tests.Desktop;

public sealed class HolidayExceptionWorkspaceViewModelTests
{
    [Fact]
    public void Constructor_InitializesCurrentYearAndEditorDefaults()
    {
        HolidayExceptionWorkspaceViewModel viewModel =
            CreateViewModel();

        Assert.Equal(
            2026,
            viewModel.SelectedYear);

        Assert.Contains(
            2026,
            viewModel.YearOptions);

        Assert.Equal(
            new DateTime(
                2026,
                8,
                23),
            viewModel.HolidayEditorDate);

        Assert.Equal(
            new DateTime(
                2026,
                8,
                23),
            viewModel.OverrideEditorWorkDate);
    }

    [Fact]
    public async Task LoadAsync_SelectsPreferredActiveScheduleAndLoadsItsOverrides()
    {
        Guid inactiveScheduleId =
            Guid.NewGuid();

        Guid activeScheduleId =
            Guid.NewGuid();

        Guid overrideId =
            Guid.NewGuid();

        var queryService =
            new FakeQueryService
            {
                Handler =
                    query =>
                    {
                        HolidayExceptionWorkspaceScheduleItem[]
                            schedules =
                            [
                                new(
                                    inactiveScheduleId,
                                    "OLD",
                                    "Lịch cũ",
                                    "SE Asia Standard Time",
                                    false),

                                new(
                                    activeScheduleId,
                                    "OFFICE",
                                    "Giờ hành chính",
                                    "SE Asia Standard Time",
                                    true)
                            ];

                        HolidayExceptionWorkspaceOverrideItem[]
                            overrides =
                                query.WorkScheduleId ==
                                    activeScheduleId
                                    ? [
                                        new(
                                            overrideId,
                                            activeScheduleId,
                                            new DateOnly(
                                                2026,
                                                9,
                                                2),
                                            true,
                                            new TimeOnly(
                                                22,
                                                0),
                                            new TimeOnly(
                                                6,
                                                0),
                                            30,
                                            450,
                                            true,
                                            "Trực ngày lễ")
                                    ]
                                    : [];

                        return new HolidayExceptionWorkspaceSnapshot(
                            query.Year,
                            query.WorkScheduleId,
                            [
                                new(
                                    Guid.NewGuid(),
                                    new DateOnly(
                                        2026,
                                        9,
                                        2),
                                    "Quốc khánh",
                                    true)
                            ],
                            schedules,
                            overrides);
                    }
            };

        HolidayExceptionWorkspaceViewModel viewModel =
            CreateViewModel(
                queryService);

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            2,
            queryService.Queries.Count);

        Assert.Null(
            queryService.Queries[0].WorkScheduleId);

        Assert.Equal(
            activeScheduleId,
            queryService.Queries[1].WorkScheduleId);

        Assert.Equal(
            activeScheduleId,
            viewModel.SelectedScheduleItem!.Id);

        Assert.Single(
            viewModel.HolidayItems);

        HolidayExceptionWorkspaceOverrideItem actualOverride =
            Assert.Single(
                viewModel.OverrideItems);

        Assert.Equal(
            overrideId,
            actualOverride.Id);
    }

    [Fact]
    public async Task SaveHolidayAsync_WithNewHoliday_CallsCreate()
    {
        var holidayService =
            new FakeHolidayManagementService();

        HolidayExceptionWorkspaceViewModel viewModel =
            CreateViewModel(
                holidayService:
                    holidayService);

        viewModel.NewHolidayCommand
            .Execute(
                null);

        viewModel.HolidayEditorDate =
            new DateTime(
                2026,
                9,
                2);

        viewModel.HolidayEditorName =
            "Quốc khánh";

        await viewModel.SaveHolidayCommand
            .ExecuteAsync(
                null);

        Assert.NotNull(
            holidayService.LastCreateRequest);

        Assert.Equal(
            new DateOnly(
                2026,
                9,
                2),
            holidayService.LastCreateRequest!.Date);

        Assert.Equal(
            "Quốc khánh",
            holidayService.LastCreateRequest.Name);

        Assert.Null(
            holidayService.LastRenameRequest);
    }

    [Fact]
    public async Task SaveHolidayAsync_WithSelectedHoliday_CallsRename()
    {
        Guid holidayId =
            Guid.NewGuid();

        var holidayService =
            new FakeHolidayManagementService();

        HolidayExceptionWorkspaceViewModel viewModel =
            CreateViewModel(
                holidayService:
                    holidayService);

        viewModel.SelectedHolidayItem =
            new HolidayExceptionWorkspaceHolidayItem(
                holidayId,
                new DateOnly(
                    2026,
                    9,
                    2),
                "Tên cũ",
                true);

        viewModel.HolidayEditorName =
            "Quốc khánh";

        await viewModel.SaveHolidayCommand
            .ExecuteAsync(
                null);

        Assert.NotNull(
            holidayService.LastRenameRequest);

        Assert.Equal(
            holidayId,
            holidayService.LastRenameRequest!
                .HolidayCalendarDayId);

        Assert.Equal(
            "Quốc khánh",
            holidayService.LastRenameRequest.Name);

        Assert.Null(
            holidayService.LastCreateRequest);
    }

    [Fact]
    public async Task HolidayActivationCommands_UseSelectedHoliday()
    {
        Guid holidayId =
            Guid.NewGuid();

        var queryService =
            new FakeQueryService
            {
                Handler =
                    query =>
                        new HolidayExceptionWorkspaceSnapshot(
                            query.Year,
                            query.WorkScheduleId,
                            [
                                new HolidayExceptionWorkspaceHolidayItem(
                                holidayId,
                                new DateOnly(
                                    2026,
                                    9,
                                    2),
                                "Quốc khánh",
                                true)
                            ],
                            [],
                            [])
            };

        var holidayService =
            new FakeHolidayManagementService();

        HolidayExceptionWorkspaceViewModel viewModel =
            CreateViewModel(
                queryService:
                    queryService,
                holidayService:
                    holidayService);

        viewModel.SelectedHolidayItem =
            new HolidayExceptionWorkspaceHolidayItem(
                holidayId,
                new DateOnly(
                    2026,
                    9,
                    2),
                "Quốc khánh",
                true);

        await viewModel.DeactivateHolidayCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            holidayId,
            holidayService.LastDeactivatedId);

        Assert.NotNull(
            viewModel.SelectedHolidayItem);

        Assert.Equal(
            holidayId,
            viewModel.SelectedHolidayItem!.Id);

        await viewModel.ReactivateHolidayCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            holidayId,
            holidayService.LastReactivatedId);
    }

    [Fact]
    public async Task SaveOverrideAsync_NewOvernightOverride_ParsesEditorValues()
    {
        Guid scheduleId =
            Guid.NewGuid();

        var overrideService =
            new FakeOverrideManagementService();

        HolidayExceptionWorkspaceViewModel viewModel =
            CreateViewModel(
                overrideService:
                    overrideService);

        viewModel.SelectedScheduleItem =
            new HolidayExceptionWorkspaceScheduleItem(
                scheduleId,
                "SECURITY",
                "Ca bảo vệ",
                "SE Asia Standard Time",
                true);

        viewModel.NewOverrideCommand
            .Execute(
                null);

        viewModel.OverrideEditorWorkDate =
            new DateTime(
                2026,
                9,
                2);

        viewModel.OverrideEditorIsWorkingDay =
            true;

        viewModel.OverrideEditorStartTimeText =
            "22:00";

        viewModel.OverrideEditorEndTimeText =
            "06:00";

        viewModel.OverrideEditorBreakMinutesText =
            "30";

        viewModel.OverrideEditorNote =
            "Trực ngày lễ";

        await viewModel.SaveOverrideCommand
            .ExecuteAsync(
                null);

        CreateWorkScheduleDateOverrideRequest request =
            Assert.IsType<CreateWorkScheduleDateOverrideRequest>(
                overrideService.LastCreateRequest);

        Assert.Equal(
            scheduleId,
            request.WorkScheduleId);

        Assert.Equal(
            new DateOnly(
                2026,
                9,
                2),
            request.WorkDate);

        Assert.Equal(
            new TimeOnly(
                22,
                0),
            request.StartTime);

        Assert.Equal(
            new TimeOnly(
                6,
                0),
            request.EndTime);

        Assert.Equal(
            30,
            request.BreakMinutes);

        Assert.Equal(
            "Trực ngày lễ",
            request.Note);
    }

    [Fact]
    public async Task SaveOverrideAsync_SelectedOverride_CallsUpdateWithoutChangingIdentity()
    {
        Guid scheduleId =
            Guid.NewGuid();

        Guid overrideId =
            Guid.NewGuid();

        var overrideService =
            new FakeOverrideManagementService();

        HolidayExceptionWorkspaceViewModel viewModel =
            CreateViewModel(
                overrideService:
                    overrideService);

        viewModel.SelectedScheduleItem =
            new HolidayExceptionWorkspaceScheduleItem(
                scheduleId,
                "OFFICE",
                "Giờ hành chính",
                "SE Asia Standard Time",
                true);

        viewModel.SelectedOverrideItem =
            new HolidayExceptionWorkspaceOverrideItem(
                overrideId,
                scheduleId,
                new DateOnly(
                    2026,
                    9,
                    2),
                true,
                new TimeOnly(
                    8,
                    0),
                new TimeOnly(
                    17,
                    0),
                60,
                480,
                false,
                "Cũ");

        viewModel.OverrideEditorStartTimeText =
            "09:00";

        viewModel.OverrideEditorEndTimeText =
            "18:00";

        viewModel.OverrideEditorBreakMinutesText =
            "45";

        viewModel.OverrideEditorNote =
            "Đã cập nhật";

        await viewModel.SaveOverrideCommand
            .ExecuteAsync(
                null);

        UpdateWorkScheduleDateOverrideRequest request =
            Assert.IsType<UpdateWorkScheduleDateOverrideRequest>(
                overrideService.LastUpdateRequest);

        Assert.Equal(
            overrideId,
            request.WorkScheduleDateOverrideId);

        Assert.Equal(
            new TimeOnly(
                9,
                0),
            request.StartTime);

        Assert.Equal(
            new TimeOnly(
                18,
                0),
            request.EndTime);

        Assert.Equal(
            45,
            request.BreakMinutes);

        Assert.Equal(
            "Đã cập nhật",
            request.Note);

        Assert.Null(
            overrideService.LastCreateRequest);
    }

    [Fact]
    public async Task DeleteOverrideAsync_UsesSelectedOverride()
    {
        Guid scheduleId =
            Guid.NewGuid();

        Guid overrideId =
            Guid.NewGuid();

        var overrideService =
            new FakeOverrideManagementService();

        HolidayExceptionWorkspaceViewModel viewModel =
            CreateViewModel(
                overrideService:
                    overrideService);

        viewModel.SelectedScheduleItem =
            new HolidayExceptionWorkspaceScheduleItem(
                scheduleId,
                "OFFICE",
                "Giờ hành chính",
                "SE Asia Standard Time",
                true);

        viewModel.SelectedOverrideItem =
            new HolidayExceptionWorkspaceOverrideItem(
                overrideId,
                scheduleId,
                new DateOnly(
                    2026,
                    9,
                    2),
                false,
                null,
                null,
                0,
                0,
                false,
                null);

        await viewModel.DeleteOverrideCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            overrideId,
            overrideService.LastDeletedId);
    }

    [Fact]
    public async Task SaveOverrideAsync_InvalidTime_FailsBeforeService()
    {
        Guid scheduleId =
            Guid.NewGuid();

        var overrideService =
            new FakeOverrideManagementService();

        HolidayExceptionWorkspaceViewModel viewModel =
            CreateViewModel(
                overrideService:
                    overrideService);

        viewModel.SelectedScheduleItem =
            new HolidayExceptionWorkspaceScheduleItem(
                scheduleId,
                "OFFICE",
                "Giờ hành chính",
                "SE Asia Standard Time",
                true);

        viewModel.NewOverrideCommand
            .Execute(
                null);

        viewModel.OverrideEditorWorkDate =
            new DateTime(
                2026,
                9,
                2);

        viewModel.OverrideEditorStartTimeText =
            "sai giờ";

        viewModel.OverrideEditorEndTimeText =
            "17:00";

        await viewModel.SaveOverrideCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            "Giờ bắt đầu phải theo định dạng HH:mm.",
            viewModel.ErrorMessage);

        Assert.Null(
            overrideService.LastCreateRequest);

        Assert.Null(
            overrideService.LastUpdateRequest);
    }

    [Fact]
    public async Task LoadAsync_WithSelectedSchedule_PreservesSelectionAndLoadsItsOverrides()
    {
        Guid scheduleId =
            Guid.NewGuid();

        Guid overrideId =
            Guid.NewGuid();

        var queryService =
            new FakeQueryService
            {
                Handler =
                    query =>
                        new HolidayExceptionWorkspaceSnapshot(
                            query.Year,
                            query.WorkScheduleId,
                            [],
                            [
                                new HolidayExceptionWorkspaceScheduleItem(
                                scheduleId,
                                "OFFICE",
                                "Giờ hành chính",
                                "SE Asia Standard Time",
                                true)
                            ],
                            query.WorkScheduleId == scheduleId
                                ? [
                                    new HolidayExceptionWorkspaceOverrideItem(
                                    overrideId,
                                    scheduleId,
                                    new DateOnly(
                                        2026,
                                        9,
                                        2),
                                    false,
                                    null,
                                    null,
                                    0,
                                    0,
                                    false,
                                    "Nghỉ riêng")
                                ]
                                : [])
            };

        HolidayExceptionWorkspaceViewModel viewModel =
            CreateViewModel(
                queryService);

        viewModel.SelectedScheduleItem =
            new HolidayExceptionWorkspaceScheduleItem(
                scheduleId,
                "OFFICE",
                "Giờ hành chính",
                "SE Asia Standard Time",
                true);

        await viewModel.LoadCommand
            .ExecuteAsync(
                null);

        Assert.Single(
            queryService.Queries);

        Assert.Equal(
            scheduleId,
            queryService.Queries[0].WorkScheduleId);

        Assert.Equal(
            scheduleId,
            viewModel.SelectedScheduleItem!.Id);

        HolidayExceptionWorkspaceOverrideItem actualOverride =
            Assert.Single(
                viewModel.OverrideItems);

        Assert.Equal(
            overrideId,
            actualOverride.Id);
    }

    [Fact]
    public async Task SaveOverrideAsync_NonWorkingDay_IgnoresStaleTimeEditorValues()
    {
        Guid scheduleId =
            Guid.NewGuid();

        var overrideService =
            new FakeOverrideManagementService();

        HolidayExceptionWorkspaceViewModel viewModel =
            CreateViewModel(
                overrideService:
                    overrideService);

        viewModel.SelectedScheduleItem =
            new HolidayExceptionWorkspaceScheduleItem(
                scheduleId,
                "OFFICE",
                "Giờ hành chính",
                "SE Asia Standard Time",
                true);

        viewModel.NewOverrideCommand
            .Execute(
                null);

        viewModel.OverrideEditorWorkDate =
            new DateTime(
                2026,
                9,
                2);

        viewModel.OverrideEditorIsWorkingDay =
            false;

        // Cố ý để dữ liệu cũ trong editor.
        viewModel.OverrideEditorStartTimeText =
            "22:00";

        viewModel.OverrideEditorEndTimeText =
            "06:00";

        viewModel.OverrideEditorBreakMinutesText =
            "30";

        await viewModel.SaveOverrideCommand
            .ExecuteAsync(
                null);

        CreateWorkScheduleDateOverrideRequest request =
            Assert.IsType<CreateWorkScheduleDateOverrideRequest>(
                overrideService.LastCreateRequest);

        Assert.False(
            request.IsWorkingDay);

        Assert.Null(
            request.StartTime);

        Assert.Null(
            request.EndTime);

        Assert.Equal(
            0,
            request.BreakMinutes);
    }

    [Fact]
    public async Task SaveHolidayAsync_ExistingHolidayWithChangedDate_FailsBeforeService()
    {
        Guid holidayId =
            Guid.NewGuid();

        var holidayService =
            new FakeHolidayManagementService();

        HolidayExceptionWorkspaceViewModel viewModel =
            CreateViewModel(
                holidayService:
                    holidayService);

        viewModel.SelectedHolidayItem =
            new HolidayExceptionWorkspaceHolidayItem(
                holidayId,
                new DateOnly(
                    2026,
                    9,
                    2),
                "Quốc khánh",
                true);

        viewModel.HolidayEditorDate =
            new DateTime(
                2026,
                9,
                3);

        viewModel.HolidayEditorName =
            "Tên mới";

        await viewModel.SaveHolidayCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            "Không thể thay đổi ngày của ngày lễ đã có.",
            viewModel.ErrorMessage);

        Assert.Null(
            holidayService.LastRenameRequest);

        Assert.Null(
            holidayService.LastCreateRequest);
    }

    [Fact]
    public async Task SaveOverrideAsync_ExistingOverrideWithChangedDate_FailsBeforeService()
    {
        Guid scheduleId =
            Guid.NewGuid();

        Guid overrideId =
            Guid.NewGuid();

        var overrideService =
            new FakeOverrideManagementService();

        HolidayExceptionWorkspaceViewModel viewModel =
            CreateViewModel(
                overrideService:
                    overrideService);

        viewModel.SelectedScheduleItem =
            new HolidayExceptionWorkspaceScheduleItem(
                scheduleId,
                "OFFICE",
                "Giờ hành chính",
                "SE Asia Standard Time",
                true);

        viewModel.SelectedOverrideItem =
            new HolidayExceptionWorkspaceOverrideItem(
                overrideId,
                scheduleId,
                new DateOnly(
                    2026,
                    9,
                    2),
                true,
                new TimeOnly(
                    8,
                    0),
                new TimeOnly(
                    17,
                    0),
                60,
                480,
                false,
                null);

        viewModel.OverrideEditorWorkDate =
            new DateTime(
                2026,
                9,
                3);

        await viewModel.SaveOverrideCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            "Không thể thay đổi ngày của ngoại lệ đã có.",
            viewModel.ErrorMessage);

        Assert.Null(
            overrideService.LastUpdateRequest);

        Assert.Null(
            overrideService.LastCreateRequest);
    }

    private static HolidayExceptionWorkspaceViewModel
        CreateViewModel(
            FakeQueryService? queryService = null,
            FakeHolidayManagementService? holidayService = null,
            FakeOverrideManagementService? overrideService = null)
    {
        return new HolidayExceptionWorkspaceViewModel(
            queryService
                ?? new FakeQueryService(),
            holidayService
                ?? new FakeHolidayManagementService(),
            overrideService
                ?? new FakeOverrideManagementService(),
            new FixedTimeProvider());
    }

    private sealed class FixedTimeProvider
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(
                2026,
                8,
                23,
                9,
                0,
                0,
                TimeSpan.Zero);
        }

        public override TimeZoneInfo LocalTimeZone =>
            TimeZoneInfo.Utc;
    }

    private sealed class FakeQueryService
        : IHolidayExceptionWorkspaceQueryService
    {
        public Func<
            HolidayExceptionWorkspaceQuery,
            HolidayExceptionWorkspaceSnapshot>
            Handler
        {
            get;
            init;
        } =
            query =>
                new HolidayExceptionWorkspaceSnapshot(
                    query.Year,
                    query.WorkScheduleId,
                    [],
                    [],
                    []);

        public List<HolidayExceptionWorkspaceQuery>
            Queries
        {
            get;
        } =
            [];

        public Task<HolidayExceptionWorkspaceSnapshot> GetAsync(
            HolidayExceptionWorkspaceQuery query,
            CancellationToken cancellationToken = default)
        {
            Queries.Add(
                query);

            return Task.FromResult(
                Handler(
                    query));
        }
    }

    private sealed class FakeHolidayManagementService
        : IHolidayCalendarManagementService
    {
        public CreateHolidayCalendarDayRequest?
            LastCreateRequest
        {
            get;
            private set;
        }

        public RenameHolidayCalendarDayRequest?
            LastRenameRequest
        {
            get;
            private set;
        }

        public Guid?
            LastDeactivatedId
        {
            get;
            private set;
        }

        public Guid?
            LastReactivatedId
        {
            get;
            private set;
        }

        public Task<HolidayCalendarManagementResult> CreateAsync(
            CreateHolidayCalendarDayRequest request,
            CancellationToken cancellationToken = default)
        {
            LastCreateRequest =
                request;

            return Task.FromResult(
                new HolidayCalendarManagementResult(
                    true,
                    Guid.NewGuid()));
        }

        public Task<HolidayCalendarManagementResult> RenameAsync(
            RenameHolidayCalendarDayRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRenameRequest =
                request;

            return Task.FromResult(
                new HolidayCalendarManagementResult(
                    true,
                    request.HolidayCalendarDayId));
        }

        public Task<HolidayCalendarManagementResult> DeactivateAsync(
            Guid holidayCalendarDayId,
            CancellationToken cancellationToken = default)
        {
            LastDeactivatedId =
                holidayCalendarDayId;

            return Task.FromResult(
                new HolidayCalendarManagementResult(
                    true,
                    holidayCalendarDayId));
        }

        public Task<HolidayCalendarManagementResult> ReactivateAsync(
            Guid holidayCalendarDayId,
            CancellationToken cancellationToken = default)
        {
            LastReactivatedId =
                holidayCalendarDayId;

            return Task.FromResult(
                new HolidayCalendarManagementResult(
                    true,
                    holidayCalendarDayId));
        }
    }

    private sealed class FakeOverrideManagementService
        : IWorkScheduleDateOverrideManagementService
    {
        public CreateWorkScheduleDateOverrideRequest?
            LastCreateRequest
        {
            get;
            private set;
        }

        public UpdateWorkScheduleDateOverrideRequest?
            LastUpdateRequest
        {
            get;
            private set;
        }

        public Guid?
            LastDeletedId
        {
            get;
            private set;
        }

        public Task<WorkScheduleDateOverrideManagementResult>
            CreateAsync(
                CreateWorkScheduleDateOverrideRequest request,
                CancellationToken cancellationToken = default)
        {
            LastCreateRequest =
                request;

            return Task.FromResult(
                new WorkScheduleDateOverrideManagementResult(
                    true,
                    Guid.NewGuid()));
        }

        public Task<WorkScheduleDateOverrideManagementResult>
            UpdateAsync(
                UpdateWorkScheduleDateOverrideRequest request,
                CancellationToken cancellationToken = default)
        {
            LastUpdateRequest =
                request;

            return Task.FromResult(
                new WorkScheduleDateOverrideManagementResult(
                    true,
                    request.WorkScheduleDateOverrideId));
        }

        public Task<WorkScheduleDateOverrideManagementResult>
            DeleteAsync(
                Guid workScheduleDateOverrideId,
                CancellationToken cancellationToken = default)
        {
            LastDeletedId =
                workScheduleDateOverrideId;

            return Task.FromResult(
                new WorkScheduleDateOverrideManagementResult(
                    true,
                    workScheduleDateOverrideId));
        }
    }
}
