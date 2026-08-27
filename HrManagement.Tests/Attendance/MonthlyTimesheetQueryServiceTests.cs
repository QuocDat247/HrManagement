using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Timesheets;

namespace HrManagement.Tests.Attendance;

public sealed class MonthlyTimesheetQueryServiceTests
{
    [Fact]
    public async Task GetAsync_WhenPeriodDoesNotExist_UsesLiveItems()
    {
        var source =
            new FakeMonthlyTimesheetQuerySource
            {
                LiveItems =
                [
                    CreateItem(
                        new DateOnly(
                            2026,
                            8,
                            21))
                ]
            };

        var service =
            new MonthlyTimesheetQueryService(
                source);

        MonthlyTimesheetReadModel result =
            await service.GetAsync(
                2026,
                8);

        Assert.Equal(
            TimesheetPeriodStatus.Open,
            result.PeriodStatus);

        Assert.Null(
            result.TimesheetPeriodId);

        Assert.False(
            result.IsClosed);

        Assert.Single(
            result.Items);

        Assert.Equal(
            1,
            source.LiveReadCount);

        Assert.Equal(
            0,
            source.ClosedReadCount);
    }

    [Fact]
    public async Task GetAsync_WhenPeriodIsOpen_UsesLiveItems()
    {
        var period =
            new TimesheetPeriod(
                Guid.NewGuid(),
                2026,
                8);

        var source =
            new FakeMonthlyTimesheetQuerySource
            {
                Period =
                    period,

                LiveItems =
                [
                    CreateItem(
                        new DateOnly(
                            2026,
                            8,
                            21))
                ]
            };

        var service =
            new MonthlyTimesheetQueryService(
                source);

        MonthlyTimesheetReadModel result =
            await service.GetAsync(
                2026,
                8);

        Assert.Equal(
            period.Id,
            result.TimesheetPeriodId);

        Assert.False(
            result.IsClosed);

        Assert.Equal(
            1,
            source.LiveReadCount);

        Assert.Equal(
            0,
            source.ClosedReadCount);
    }

    [Fact]
    public async Task GetAsync_WhenPeriodIsClosed_UsesSnapshots()
    {
        var period =
            new TimesheetPeriod(
                Guid.NewGuid(),
                2026,
                8);

        period.Close(
            new DateTime(
                2026,
                8,
                31,
                12,
                0,
                0,
                DateTimeKind.Utc),
            "user-1",
            "admin");

        var source =
            new FakeMonthlyTimesheetQuerySource
            {
                Period =
                    period,

                ClosedItems =
                [
                    CreateItem(
                        new DateOnly(
                            2026,
                            8,
                            21))
                ]
            };

        var service =
            new MonthlyTimesheetQueryService(
                source);

        MonthlyTimesheetReadModel result =
            await service.GetAsync(
                2026,
                8);

        Assert.True(
            result.IsClosed);

        Assert.Equal(
            TimesheetPeriodStatus.Closed,
            result.PeriodStatus);

        Assert.Equal(
            period.Id,
            result.TimesheetPeriodId);

        Assert.Equal(
            period.ClosedAtUtc,
            result.ClosedAtUtc);

        Assert.Equal(
            "user-1",
            result.ClosedByUserId);

        Assert.Equal(
            "admin",
            result.ClosedByUsername);

        Assert.Equal(
            0,
            source.LiveReadCount);

        Assert.Equal(
            1,
            source.ClosedReadCount);
    }

    [Fact]
    public async Task GetAsync_PassesWholeCalendarMonthToLiveSource()
    {
        var source =
            new FakeMonthlyTimesheetQuerySource();

        var service =
            new MonthlyTimesheetQueryService(
                source);

        await service.GetAsync(
            2024,
            2);

        Assert.Equal(
            new DateOnly(
                2024,
                2,
                1),
            source.LastStartDate);

        Assert.Equal(
            new DateOnly(
                2024,
                2,
                29),
            source.LastEndDate);
    }

    [Fact]
    public async Task GetAsync_WhenMonthIsInvalid_Throws()
    {
        var service =
            new MonthlyTimesheetQueryService(
                new FakeMonthlyTimesheetQuerySource());

        await Assert.ThrowsAsync<
            ArgumentOutOfRangeException>(
                () =>
                    service.GetAsync(
                        2026,
                        13));
    }

    private static MonthlyTimesheetDayItem CreateItem(
        DateOnly workDate)
    {
        return new MonthlyTimesheetDayItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            workDate,
            true,
            480,
            AttendanceCalculationStatus.Present,
            480,
            0,
            0,
            0);
    }

    private sealed class FakeMonthlyTimesheetQuerySource
        : IMonthlyTimesheetQuerySource
    {
        public TimesheetPeriod? Period
        {
            get;
            init;
        }

        public IReadOnlyList<MonthlyTimesheetDayItem>
            LiveItems
        {
            get;
            init;
        } = [];

        public IReadOnlyList<MonthlyTimesheetDayItem>
            ClosedItems
        {
            get;
            init;
        } = [];

        public int LiveReadCount
        {
            get;
            private set;
        }

        public int ClosedReadCount
        {
            get;
            private set;
        }

        public DateOnly? LastStartDate
        {
            get;
            private set;
        }

        public DateOnly? LastEndDate
        {
            get;
            private set;
        }

        public Task<TimesheetPeriod?> GetPeriodAsync(
            int year,
            int month,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Period);
        }

        public Task<IReadOnlyList<MonthlyTimesheetDayItem>>
            GetLiveItemsAsync(
                DateOnly startDate,
                DateOnly endDate,
                CancellationToken cancellationToken = default)
        {
            LiveReadCount++;

            LastStartDate =
                startDate;

            LastEndDate =
                endDate;

            return Task.FromResult(
                LiveItems);
        }

        public Task<IReadOnlyList<MonthlyTimesheetDayItem>>
            GetClosedItemsAsync(
                Guid timesheetPeriodId,
                CancellationToken cancellationToken = default)
        {
            ClosedReadCount++;

            return Task.FromResult(
                ClosedItems);
        }
    }
}
