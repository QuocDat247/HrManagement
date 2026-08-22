using HrManagement.Application.Attendance.Schedules;
using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Tests.Attendance;

public sealed class WorkScheduleDayManagementServiceTests
{
    [Fact]
    public async Task UpdateAsync_WorkingDayUpdatesTimesAndBreak()
    {
        TestContext context =
            CreateContext();

        WorkScheduleDayManagementResult result =
            await context.Service.UpdateAsync(
                new UpdateWorkScheduleDayRequest(
                    context.Schedule.Id,
                    DayOfWeek.Monday,
                    true,
                    new TimeOnly(
                        8,
                        30),
                    new TimeOnly(
                        17,
                        30),
                    60));

        Assert.True(
            result.IsSuccessful);

        WorkScheduleDay updated =
            Assert.IsType<WorkScheduleDay>(
                context.DayPersistence.LastUpdated);

        Assert.True(
            updated.IsWorkingDay);

        Assert.Equal(
            new TimeOnly(
                8,
                30),
            updated.StartTime);

        Assert.Equal(
            new TimeOnly(
                17,
                30),
            updated.EndTime);

        Assert.Equal(
            60,
            updated.BreakMinutes);

        Assert.Equal(
            480,
            updated.PlannedMinutes);
    }

    [Fact]
    public async Task UpdateAsync_NonWorkingDayClearsScheduleFields()
    {
        TestContext context =
            CreateContext();

        WorkScheduleDayManagementResult result =
            await context.Service.UpdateAsync(
                new UpdateWorkScheduleDayRequest(
                    context.Schedule.Id,
                    DayOfWeek.Monday,
                    false,
                    null,
                    null,
                    0));

        Assert.True(
            result.IsSuccessful);

        WorkScheduleDay updated =
            Assert.IsType<WorkScheduleDay>(
                context.DayPersistence.LastUpdated);

        Assert.False(
            updated.IsWorkingDay);

        Assert.Null(
            updated.StartTime);

        Assert.Null(
            updated.EndTime);

        Assert.Equal(
            0,
            updated.BreakMinutes);

        Assert.Equal(
            0,
            updated.PlannedMinutes);
    }

    [Fact]
    public async Task UpdateAsync_WorkingDayRequiresStartAndEnd()
    {
        TestContext context =
            CreateContext();

        WorkScheduleDayManagementResult result =
            await context.Service.UpdateAsync(
                new UpdateWorkScheduleDayRequest(
                    context.Schedule.Id,
                    DayOfWeek.Monday,
                    true,
                    null,
                    null,
                    60));

        Assert.False(
            result.IsSuccessful);

        Assert.Contains(
            "giờ bắt đầu",
            result.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);

        Assert.Null(
            context.DayPersistence.LastUpdated);
    }

    [Fact]
    public async Task UpdateAsync_BreakCannotReachShiftLength()
    {
        TestContext context =
            CreateContext();

        WorkScheduleDayManagementResult result =
            await context.Service.UpdateAsync(
                new UpdateWorkScheduleDayRequest(
                    context.Schedule.Id,
                    DayOfWeek.Monday,
                    true,
                    new TimeOnly(
                        8,
                        0),
                    new TimeOnly(
                        12,
                        0),
                    240));

        Assert.False(
            result.IsSuccessful);

        Assert.Contains(
            "ngắn hơn",
            result.ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_OvernightShiftIsSupported()
    {
        TestContext context =
            CreateContext();

        WorkScheduleDayManagementResult result =
            await context.Service.UpdateAsync(
                new UpdateWorkScheduleDayRequest(
                    context.Schedule.Id,
                    DayOfWeek.Monday,
                    true,
                    new TimeOnly(
                        22,
                        0),
                    new TimeOnly(
                        6,
                        0),
                    30));

        Assert.True(
            result.IsSuccessful);

        WorkScheduleDay updated =
            Assert.IsType<WorkScheduleDay>(
                context.DayPersistence.LastUpdated);

        Assert.True(
            updated.IsOvernight);

        Assert.Equal(
            450,
            updated.PlannedMinutes);
    }

    [Fact]
    public async Task UpdateAsync_MissingScheduleFails()
    {
        TestContext context =
            CreateContext();

        context.SchedulePersistence.Schedules.Clear();

        WorkScheduleDayManagementResult result =
            await context.Service.UpdateAsync(
                new UpdateWorkScheduleDayRequest(
                    context.Schedule.Id,
                    DayOfWeek.Monday,
                    false,
                    null,
                    null,
                    0));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Không tìm thấy lịch làm việc.",
            result.ErrorMessage);
    }

    private static TestContext CreateContext()
    {
        WorkSchedule schedule =
            new(
                Guid.NewGuid(),
                "TEST",
                "Lịch kiểm thử",
                "SE Asia Standard Time",
                false);

        WorkScheduleDay monday =
            new(
                Guid.NewGuid(),
                schedule.Id,
                DayOfWeek.Monday,
                true,
                new TimeOnly(
                    8,
                    0),
                new TimeOnly(
                    17,
                    0),
                60);

        var schedulePersistence =
            new TestSchedulePersistence();

        schedulePersistence.Schedules.Add(
            schedule);

        var dayPersistence =
            new TestDayPersistence
            {
                Day =
                    monday
            };

        var service =
            new WorkScheduleDayManagementService(
                schedulePersistence,
                dayPersistence);

        return new TestContext(
            service,
            schedulePersistence,
            dayPersistence,
            schedule);
    }

    private sealed record TestContext(
        WorkScheduleDayManagementService Service,
        TestSchedulePersistence SchedulePersistence,
        TestDayPersistence DayPersistence,
        WorkSchedule Schedule);

    private sealed class TestSchedulePersistence
        : IWorkScheduleManagementPersistence
    {
        public bool IsInUse
        {
            get;
            set;
        }

        public Guid? DeletedWorkScheduleId
        {
            get;
            private set;
        }

        public Task<bool> IsInUseAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                IsInUse);
        }

        public Task DeleteAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken = default)
        {
            DeletedWorkScheduleId =
                workScheduleId;

            Schedules.RemoveAll(
                schedule =>
                    schedule.Id ==
                    workScheduleId);

            return Task.CompletedTask;
        }

        public List<WorkSchedule> Schedules
        {
            get;
        } = [];

        public Task<WorkSchedule?> GetByIdAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Schedules.SingleOrDefault(
                    schedule =>
                        schedule.Id ==
                        workScheduleId));
        }

        public Task<WorkSchedule?> GetByCodeAsync(
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<WorkSchedule?>(
                null);
        }

        public Task CreateAsync(
            WorkSchedule schedule,
            IReadOnlyList<WorkScheduleDay> days,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(
            WorkSchedule schedule,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestDayPersistence
        : IWorkScheduleDayManagementPersistence
    {
        public WorkScheduleDay? Day
        {
            get;
            init;
        }

        public WorkScheduleDay? LastUpdated
        {
            get;
            private set;
        }

        public Task<WorkScheduleDay?> GetAsync(
            Guid workScheduleId,
            DayOfWeek dayOfWeek,
            CancellationToken cancellationToken = default)
        {
            WorkScheduleDay? result =
                Day is not null
                && Day.WorkScheduleId ==
                    workScheduleId
                && Day.DayOfWeek ==
                    dayOfWeek
                    ? Day
                    : null;

            return Task.FromResult(
                result);
        }

        public Task UpdateAsync(
            WorkScheduleDay day,
            CancellationToken cancellationToken = default)
        {
            LastUpdated =
                day;

            return Task.CompletedTask;
        }
    }
}
