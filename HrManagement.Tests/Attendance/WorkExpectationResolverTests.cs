using HrManagement.Application.Attendance.Expectations;
using HrManagement.Domain.Attendance.Calendars;
using HrManagement.Domain.Attendance.Expectations;
using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Tests.Attendance;

public sealed class WorkExpectationResolverTests
{
    [Fact]
    public async Task ResolveAsync_DateOverrideBeatsHolidayAndWeeklySchedule()
    {
        Guid scheduleId =
            Guid.NewGuid();

        DateOnly workDate =
            new(
                2026,
                9,
                2);

        WorkScheduleDay weeklyDay =
            CreateWorkingWeeklyDay(
                scheduleId,
                workDate.DayOfWeek);

        HolidayCalendarDay holiday =
            CreateHoliday(
                workDate);

        WorkScheduleDateOverride dateOverride =
            new(
                Guid.NewGuid(),
                scheduleId,
                workDate,
                true,
                new TimeOnly(
                    22,
                    0),
                new TimeOnly(
                    6,
                    0),
                30,
                "Trực ngày lễ");

        var persistence =
            new FakePersistence
            {
                Data =
                    new WorkExpectationResolutionData(
                        holiday,
                        new[]
                        {
                            weeklyDay
                        },
                        new[]
                        {
                            dateOverride
                        })
            };

        var resolver =
            new WorkExpectationResolver(
                persistence);

        ResolvedWorkExpectation expectation =
            Assert.IsType<ResolvedWorkExpectation>(
                await resolver.ResolveAsync(
                    scheduleId,
                    workDate));

        Assert.Equal(
            WorkExpectationSource.DateOverride,
            expectation.Source);

        Assert.Equal(
            dateOverride.Id,
            expectation.SourceId);

        Assert.Equal(
            "Trực ngày lễ",
            expectation.SourceName);

        Assert.True(
            expectation.IsWorkingDay);

        Assert.True(
            expectation.IsOvernight);

        Assert.Equal(
            450,
            expectation.PlannedMinutes);
    }

    [Fact]
    public async Task ResolveAsync_ActiveHolidayBeatsWeeklySchedule()
    {
        Guid scheduleId =
            Guid.NewGuid();

        DateOnly workDate =
            new(
                2026,
                9,
                2);

        HolidayCalendarDay holiday =
            CreateHoliday(
                workDate);

        var persistence =
            new FakePersistence
            {
                Data =
                    new WorkExpectationResolutionData(
                        holiday,
                        new[]
                        {
                            CreateWorkingWeeklyDay(
                                scheduleId,
                                workDate.DayOfWeek)
                        },
                        Array.Empty<WorkScheduleDateOverride>())
            };

        var resolver =
            new WorkExpectationResolver(
                persistence);

        ResolvedWorkExpectation expectation =
            Assert.IsType<ResolvedWorkExpectation>(
                await resolver.ResolveAsync(
                    scheduleId,
                    workDate));

        Assert.Equal(
            WorkExpectationSource.Holiday,
            expectation.Source);

        Assert.Equal(
            holiday.Id,
            expectation.SourceId);

        Assert.Equal(
            "Quốc khánh",
            expectation.SourceName);

        Assert.False(
            expectation.IsWorkingDay);

        Assert.Null(
            expectation.StartTime);

        Assert.Null(
            expectation.EndTime);

        Assert.Equal(
            0,
            expectation.PlannedMinutes);
    }

    [Fact]
    public async Task ResolveAsync_InactiveHolidayFallsBackToWeeklySchedule()
    {
        Guid scheduleId =
            Guid.NewGuid();

        DateOnly workDate =
            new(
                2026,
                9,
                2);

        HolidayCalendarDay holiday =
            CreateHoliday(
                workDate,
                isActive: false);

        WorkScheduleDay weeklyDay =
            CreateWorkingWeeklyDay(
                scheduleId,
                workDate.DayOfWeek);

        var resolver =
            new WorkExpectationResolver(
                new FakePersistence
                {
                    Data =
                        new WorkExpectationResolutionData(
                            holiday,
                            new[]
                            {
                                weeklyDay
                            },
                            Array.Empty<WorkScheduleDateOverride>())
                });

        ResolvedWorkExpectation expectation =
            Assert.IsType<ResolvedWorkExpectation>(
                await resolver.ResolveAsync(
                    scheduleId,
                    workDate));

        Assert.Equal(
            WorkExpectationSource.WeeklySchedule,
            expectation.Source);

        Assert.Equal(
            weeklyDay.Id,
            expectation.SourceId);

        Assert.True(
            expectation.IsWorkingDay);

        Assert.Equal(
            480,
            expectation.PlannedMinutes);
    }

    [Fact]
    public async Task ResolveAsync_WithoutHigherPrioritySource_UsesWeeklySchedule()
    {
        Guid scheduleId =
            Guid.NewGuid();

        DateOnly workDate =
            new(
                2026,
                9,
                3);

        WorkScheduleDay weeklyDay =
            CreateWorkingWeeklyDay(
                scheduleId,
                workDate.DayOfWeek);

        var resolver =
            new WorkExpectationResolver(
                new FakePersistence
                {
                    Data =
                        new WorkExpectationResolutionData(
                            null,
                            new[]
                            {
                                weeklyDay
                            },
                            Array.Empty<WorkScheduleDateOverride>())
                });

        ResolvedWorkExpectation expectation =
            Assert.IsType<ResolvedWorkExpectation>(
                await resolver.ResolveAsync(
                    scheduleId,
                    workDate));

        Assert.Equal(
            WorkExpectationSource.WeeklySchedule,
            expectation.Source);

        Assert.Equal(
            new TimeOnly(
                8,
                0),
            expectation.StartTime);

        Assert.Equal(
            new TimeOnly(
                17,
                0),
            expectation.EndTime);

        Assert.Equal(
            60,
            expectation.BreakMinutes);

        Assert.Equal(
            480,
            expectation.PlannedMinutes);
    }

    [Fact]
    public async Task ResolveAsync_WhenNoApplicableSource_ReturnsNull()
    {
        Guid scheduleId =
            Guid.NewGuid();

        DateOnly workDate =
            new(
                2026,
                9,
                3);

        var resolver =
            new WorkExpectationResolver(
                new FakePersistence
                {
                    Data =
                        new WorkExpectationResolutionData(
                            null,
                            Array.Empty<WorkScheduleDay>(),
                            Array.Empty<WorkScheduleDateOverride>())
                });

        ResolvedWorkExpectation? expectation =
            await resolver.ResolveAsync(
                scheduleId,
                workDate);

        Assert.Null(
            expectation);
    }

    [Fact]
    public async Task ResolveManyAsync_AppliesPrecedenceIndependentlyPerSchedule()
    {
        Guid overriddenScheduleId =
            Guid.NewGuid();

        Guid holidayScheduleId =
            Guid.NewGuid();

        DateOnly workDate =
            new(
                2026,
                9,
                2);

        HolidayCalendarDay holiday =
            CreateHoliday(
                workDate);

        WorkScheduleDateOverride dateOverride =
            new(
                Guid.NewGuid(),
                overriddenScheduleId,
                workDate,
                true,
                new TimeOnly(
                    22,
                    0),
                new TimeOnly(
                    6,
                    0),
                30,
                "Trực ngày lễ");

        var persistence =
            new FakePersistence
            {
                Data =
                    new WorkExpectationResolutionData(
                        holiday,
                        new[]
                        {
                            CreateWorkingWeeklyDay(
                                overriddenScheduleId,
                                workDate.DayOfWeek),
                            CreateWorkingWeeklyDay(
                                holidayScheduleId,
                                workDate.DayOfWeek)
                        },
                        new[]
                        {
                            dateOverride
                        })
            };

        var resolver =
            new WorkExpectationResolver(
                persistence);

        IReadOnlyDictionary<Guid, ResolvedWorkExpectation>
            result =
                await resolver.ResolveManyAsync(
                    new[]
                    {
                        overriddenScheduleId,
                        holidayScheduleId
                    },
                    workDate);

        Assert.Equal(
            2,
            result.Count);

        Assert.Equal(
            WorkExpectationSource.DateOverride,
            result[overriddenScheduleId].Source);

        Assert.True(
            result[overriddenScheduleId].IsWorkingDay);

        Assert.Equal(
            WorkExpectationSource.Holiday,
            result[holidayScheduleId].Source);

        Assert.False(
            result[holidayScheduleId].IsWorkingDay);

        Assert.Equal(
            1,
            persistence.LoadCallCount);
    }

    [Fact]
    public async Task ResolveManyAsync_WithEmptyScheduleList_DoesNotLoadPersistence()
    {
        var persistence =
            new FakePersistence();

        var resolver =
            new WorkExpectationResolver(
                persistence);

        IReadOnlyDictionary<Guid, ResolvedWorkExpectation>
            result =
                await resolver.ResolveManyAsync(
                    Array.Empty<Guid>(),
                    new DateOnly(
                        2026,
                        9,
                        2));

        Assert.Empty(
            result);

        Assert.Equal(
            0,
            persistence.LoadCallCount);
    }

    [Fact]
    public async Task ResolveAsync_WithEmptyScheduleId_ThrowsBeforePersistence()
    {
        var persistence =
            new FakePersistence();

        var resolver =
            new WorkExpectationResolver(
                persistence);

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                resolver.ResolveAsync(
                    Guid.Empty,
                    new DateOnly(
                        2026,
                        9,
                        2)));

        Assert.Equal(
            0,
            persistence.LoadCallCount);
    }

    [Fact]
    public async Task ResolveAsync_WithDefaultDate_ThrowsBeforePersistence()
    {
        var persistence =
            new FakePersistence();

        var resolver =
            new WorkExpectationResolver(
                persistence);

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                resolver.ResolveAsync(
                    Guid.NewGuid(),
                    default));

        Assert.Equal(
            0,
            persistence.LoadCallCount);
    }

    private static HolidayCalendarDay CreateHoliday(
        DateOnly workDate,
        bool isActive = true)
    {
        return new HolidayCalendarDay(
            Guid.NewGuid(),
            workDate,
            "Quốc khánh",
            isActive);
    }

    [Fact]
    public async Task ResolveAsync_WithDuplicateDateOverrides_ThrowsClearError()
    {
        Guid scheduleId =
            Guid.NewGuid();

        DateOnly workDate =
            new(
                2026,
                9,
                2);

        var first =
            new WorkScheduleDateOverride(
                Guid.NewGuid(),
                scheduleId,
                workDate,
                false);

        var second =
            new WorkScheduleDateOverride(
                Guid.NewGuid(),
                scheduleId,
                workDate,
                true,
                new TimeOnly(
                    8,
                    0),
                new TimeOnly(
                    17,
                    0),
                60);

        var resolver =
            new WorkExpectationResolver(
                new FakePersistence
                {
                    Data =
                        new WorkExpectationResolutionData(
                            null,
                            Array.Empty<WorkScheduleDay>(),
                            new[]
                            {
                            first,
                            second
                            })
                });

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    resolver.ResolveAsync(
                        scheduleId,
                        workDate));

        Assert.Equal(
            "Phát hiện nhiều ngoại lệ lịch làm việc cho cùng lịch và ngày.",
            exception.Message);
    }

    [Fact]
    public async Task ResolveAsync_WithDuplicateWeeklyDays_ThrowsClearError()
    {
        Guid scheduleId =
            Guid.NewGuid();

        DateOnly workDate =
            new(
                2026,
                9,
                3);

        WorkScheduleDay first =
            CreateWorkingWeeklyDay(
                scheduleId,
                workDate.DayOfWeek);

        WorkScheduleDay second =
            CreateWorkingWeeklyDay(
                scheduleId,
                workDate.DayOfWeek);

        var resolver =
            new WorkExpectationResolver(
                new FakePersistence
                {
                    Data =
                        new WorkExpectationResolutionData(
                            null,
                            new[]
                            {
                            first,
                            second
                            },
                            Array.Empty<WorkScheduleDateOverride>())
                });

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    resolver.ResolveAsync(
                        scheduleId,
                        workDate));

        Assert.Equal(
            "Phát hiện nhiều cấu hình ngày trong tuần cho cùng lịch làm việc.",
            exception.Message);
    }

    [Fact]
    public async Task ResolveManyAsync_WithDuplicateScheduleIds_LoadsUniqueSchedulesOnly()
    {
        Guid scheduleId =
            Guid.NewGuid();

        DateOnly workDate =
            new(
                2026,
                9,
                3);

        var persistence =
            new FakePersistence
            {
                Data =
                    new WorkExpectationResolutionData(
                        null,
                        new[]
                        {
                        CreateWorkingWeeklyDay(
                            scheduleId,
                            workDate.DayOfWeek)
                        },
                        Array.Empty<WorkScheduleDateOverride>())
            };

        var resolver =
            new WorkExpectationResolver(
                persistence);

        IReadOnlyDictionary<Guid, ResolvedWorkExpectation>
            result =
                await resolver.ResolveManyAsync(
                    new[]
                    {
                    scheduleId,
                    scheduleId,
                    scheduleId
                    },
                    workDate);

        Assert.Single(
            result);

        Assert.Equal(
            1,
            persistence.LoadCallCount);

        Guid loadedScheduleId =
            Assert.Single(
                persistence.LastWorkScheduleIds);

        Assert.Equal(
            scheduleId,
            loadedScheduleId);
    }

    [Fact]
    public async Task ResolveAsync_WithHolidayForDifferentDate_IgnoresHoliday()
    {
        Guid scheduleId =
            Guid.NewGuid();

        DateOnly workDate =
            new(
                2026,
                9,
                3);

        WorkScheduleDay weeklyDay =
            CreateWorkingWeeklyDay(
                scheduleId,
                workDate.DayOfWeek);

        HolidayCalendarDay wrongDateHoliday =
            CreateHoliday(
                workDate.AddDays(
                    -1));

        var resolver =
            new WorkExpectationResolver(
                new FakePersistence
                {
                    Data =
                        new WorkExpectationResolutionData(
                            wrongDateHoliday,
                            new[]
                            {
                            weeklyDay
                            },
                            Array.Empty<WorkScheduleDateOverride>())
                });

        ResolvedWorkExpectation expectation =
            Assert.IsType<ResolvedWorkExpectation>(
                await resolver.ResolveAsync(
                    scheduleId,
                    workDate));

        Assert.Equal(
            WorkExpectationSource.WeeklySchedule,
            expectation.Source);

        Assert.Equal(
            weeklyDay.Id,
            expectation.SourceId);

        Assert.True(
            expectation.IsWorkingDay);
    }
    private static WorkScheduleDay CreateWorkingWeeklyDay(
        Guid workScheduleId,
        DayOfWeek dayOfWeek)
    {
        return new WorkScheduleDay(
            Guid.NewGuid(),
            workScheduleId,
            dayOfWeek,
            true,
            new TimeOnly(
                8,
                0),
            new TimeOnly(
                17,
                0),
            60);
    }

    private sealed class FakePersistence
        : IWorkExpectationResolutionPersistence
    {
        public IReadOnlyCollection<Guid> LastWorkScheduleIds
        {
            get;
            private set;
        } =
            Array.Empty<Guid>();

        public WorkExpectationResolutionData Data
        {
            get;
            init;
        } =
            new(
                null,
                Array.Empty<WorkScheduleDay>(),
                Array.Empty<WorkScheduleDateOverride>());

        public int LoadCallCount
        {
            get;
            private set;
        }

        public Task<WorkExpectationResolutionData> LoadAsync(
            DateOnly workDate,
            IReadOnlyCollection<Guid> workScheduleIds,
            CancellationToken cancellationToken = default)
        {
            LoadCallCount++;

            LastWorkScheduleIds =
                workScheduleIds
                    .ToArray();

            return Task.FromResult(
                Data);
        }
    }
}
