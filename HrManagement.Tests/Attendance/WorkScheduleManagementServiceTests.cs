using HrManagement.Application.Attendance.Schedules;
using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Tests.Attendance;

public sealed class WorkScheduleManagementServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesInactiveScheduleWithSevenDays()
    {
        var persistence =
            new TestPersistence();

        var dayRepository =
            new TestDayRepository();

        var service =
            new WorkScheduleManagementService(
                persistence,
                dayRepository);

        WorkScheduleManagementResult result =
            await service.CreateAsync(
                new CreateWorkScheduleRequest(
                    " night ",
                    " Ca đêm ",
                    "SE Asia Standard Time"));

        Assert.True(
            result.IsSuccessful);

        Assert.NotNull(
            result.WorkScheduleId);

        WorkSchedule schedule =
            Assert.Single(
                persistence.Schedules);

        Assert.Equal(
            "NIGHT",
            schedule.Code);

        Assert.Equal(
            "Ca đêm",
            schedule.Name);

        Assert.False(
            schedule.IsActive);

        Assert.Equal(
            7,
            persistence.CreatedDays.Count);

        Assert.All(
            persistence.CreatedDays,
            day =>
            {
                Assert.Equal(
                    schedule.Id,
                    day.WorkScheduleId);

                Assert.False(
                    day.IsWorkingDay);
            });

        Assert.Equal(
            7,
            persistence.CreatedDays
                .Select(
                    day =>
                        day.DayOfWeek)
                .Distinct()
                .Count());
    }

    [Fact]
    public async Task CreateAsync_DuplicateCodeFails()
    {
        var persistence =
            new TestPersistence();

        persistence.Schedules.Add(
            CreateSchedule(
                "OFFICE",
                isActive: true));

        var service =
            new WorkScheduleManagementService(
                persistence,
                new TestDayRepository());

        WorkScheduleManagementResult result =
            await service.CreateAsync(
                new CreateWorkScheduleRequest(
                    "office",
                    "Trùng mã",
                    "SE Asia Standard Time"));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Mã lịch làm việc đã tồn tại.",
            result.ErrorMessage);

        Assert.Single(
            persistence.Schedules);
    }

    [Fact]
    public async Task UpdateAsync_PreservesActiveState()
    {
        var persistence =
            new TestPersistence();

        WorkSchedule existing =
            CreateSchedule(
                "OFFICE",
                isActive: true);

        persistence.Schedules.Add(
            existing);

        var service =
            new WorkScheduleManagementService(
                persistence,
                new TestDayRepository());

        WorkScheduleManagementResult result =
            await service.UpdateAsync(
                new UpdateWorkScheduleRequest(
                    existing.Id,
                    "OFFICE-NEW",
                    "Giờ hành chính mới",
                    "SE Asia Standard Time"));

        Assert.True(
            result.IsSuccessful);

        WorkSchedule updated =
            Assert.Single(
                persistence.Schedules);

        Assert.Equal(
            existing.Id,
            updated.Id);

        Assert.Equal(
            "OFFICE-NEW",
            updated.Code);

        Assert.Equal(
            "Giờ hành chính mới",
            updated.Name);

        Assert.True(
            updated.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_MakesScheduleInactive()
    {
        var persistence =
            new TestPersistence();

        WorkSchedule existing =
            CreateSchedule(
                "OFFICE",
                isActive: true);

        persistence.Schedules.Add(
            existing);

        var service =
            new WorkScheduleManagementService(
                persistence,
                new TestDayRepository());

        WorkScheduleManagementResult result =
            await service.DeactivateAsync(
                existing.Id);

        Assert.True(
            result.IsSuccessful);

        Assert.False(
            Assert.Single(
                    persistence.Schedules)
                .IsActive);
    }

    [Fact]
    public async Task ReactivateAsync_WithoutWorkingDayFails()
    {
        var persistence =
            new TestPersistence();

        WorkSchedule existing =
            CreateSchedule(
                "NEW",
                isActive: false);

        persistence.Schedules.Add(
            existing);

        var dayRepository =
            new TestDayRepository();

        dayRepository.Days =
            Enum.GetValues<DayOfWeek>()
                .Select(
                    day =>
                        new WorkScheduleDay(
                            Guid.NewGuid(),
                            existing.Id,
                            day,
                            isWorkingDay: false))
                .ToArray();

        var service =
            new WorkScheduleManagementService(
                persistence,
                dayRepository);

        WorkScheduleManagementResult result =
            await service.ReactivateAsync(
                existing.Id);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Lịch làm việc phải có ít nhất một ngày làm việc trước khi kích hoạt.",
            result.ErrorMessage);

        Assert.False(
            Assert.Single(
                    persistence.Schedules)
                .IsActive);
    }

    [Fact]
    public async Task ReactivateAsync_WithWorkingDayActivatesSchedule()
    {
        var persistence =
            new TestPersistence();

        WorkSchedule existing =
            CreateSchedule(
                "NEW",
                isActive: false);

        persistence.Schedules.Add(
            existing);

        var days =
            Enum.GetValues<DayOfWeek>()
                .Select(
                    day =>
                        new WorkScheduleDay(
                            Guid.NewGuid(),
                            existing.Id,
                            day,
                            isWorkingDay: false))
                .ToList();

        days[0] =
            new WorkScheduleDay(
                days[0].Id,
                existing.Id,
                days[0].DayOfWeek,
                isWorkingDay: true,
                startTime:
                    new TimeOnly(
                        8,
                        0),
                endTime:
                    new TimeOnly(
                        17,
                        0),
                breakMinutes:
                    60);

        var dayRepository =
            new TestDayRepository
            {
                Days =
                    days
            };

        var service =
            new WorkScheduleManagementService(
                persistence,
                dayRepository);

        WorkScheduleManagementResult result =
            await service.ReactivateAsync(
                existing.Id);

        Assert.True(
            result.IsSuccessful);

        Assert.True(
            Assert.Single(
                    persistence.Schedules)
                .IsActive);
    }

    [Fact]
    public async Task CloneAsync_CopiesScheduleAndDaysAsInactive()
    {
        var persistence =
            new TestPersistence();

        WorkSchedule source =
            CreateSchedule(
                "OFFICE",
                isActive: true);

        persistence.Schedules.Add(
            source);

        var sourceDays =
            Enum.GetValues<DayOfWeek>()
                .Select(
                    day =>
                        day == DayOfWeek.Monday
                            ? new WorkScheduleDay(
                                Guid.NewGuid(),
                                source.Id,
                                day,
                                true,
                                new TimeOnly(
                                    8,
                                    0),
                                new TimeOnly(
                                    17,
                                    0),
                                60)
                            : new WorkScheduleDay(
                                Guid.NewGuid(),
                                source.Id,
                                day,
                                false))
                .ToArray();

        var dayRepository =
            new TestDayRepository
            {
                Days =
                    sourceDays
            };

        var service =
            new WorkScheduleManagementService(
                persistence,
                dayRepository);

        WorkScheduleManagementResult result =
            await service.CloneAsync(
                new CloneWorkScheduleRequest(
                    source.Id,
                    "OFFICE-ALT",
                    "Ca hành chính thay thế"));

        Assert.True(
            result.IsSuccessful);

        Assert.NotNull(
            result.WorkScheduleId);

        WorkSchedule clone =
            persistence.Schedules.Single(
                schedule =>
                    schedule.Id ==
                    result.WorkScheduleId);

        Assert.Equal(
            "OFFICE-ALT",
            clone.Code);

        Assert.Equal(
            "Ca hành chính thay thế",
            clone.Name);

        Assert.Equal(
            source.TimeZoneId,
            clone.TimeZoneId);

        Assert.False(
            clone.IsActive);

        Assert.NotEqual(
            source.Id,
            clone.Id);

        Assert.Equal(
            7,
            persistence.CreatedDays.Count);

        WorkScheduleDay clonedMonday =
            persistence.CreatedDays.Single(
                day =>
                    day.DayOfWeek ==
                    DayOfWeek.Monday);

        Assert.True(
            clonedMonday.IsWorkingDay);

        Assert.Equal(
            new TimeOnly(
                8,
                0),
            clonedMonday.StartTime);

        Assert.Equal(
            new TimeOnly(
                17,
                0),
            clonedMonday.EndTime);

        Assert.Equal(
            60,
            clonedMonday.BreakMinutes);

        Assert.All(
            persistence.CreatedDays,
            day =>
                Assert.Equal(
                    clone.Id,
                    day.WorkScheduleId));

        Assert.Empty(
            persistence.CreatedDays
                .Select(
                    day =>
                        day.Id)
                .Intersect(
                    sourceDays.Select(
                        day =>
                            day.Id)));
    }

    [Fact]
    public async Task CloneAsync_MissingSourceFails()
    {
        var service =
            new WorkScheduleManagementService(
                new TestPersistence(),
                new TestDayRepository());

        WorkScheduleManagementResult result =
            await service.CloneAsync(
                new CloneWorkScheduleRequest(
                    Guid.NewGuid(),
                    "COPY",
                    "Bản sao"));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Không tìm thấy mẫu lịch nguồn.",
            result.ErrorMessage);
    }

    [Fact]
    public async Task CloneAsync_DuplicateCodeFails()
    {
        var persistence =
            new TestPersistence();

        WorkSchedule source =
            CreateSchedule(
                "OFFICE",
                isActive: true);

        persistence.Schedules.Add(
            source);

        persistence.Schedules.Add(
            CreateSchedule(
                "COPY",
                isActive: false));

        var dayRepository =
            new TestDayRepository
            {
                Days =
                    Enum.GetValues<DayOfWeek>()
                        .Select(
                            day =>
                                new WorkScheduleDay(
                                    Guid.NewGuid(),
                                    source.Id,
                                    day,
                                    false))
                        .ToArray()
            };

        var service =
            new WorkScheduleManagementService(
                persistence,
                dayRepository);

        WorkScheduleManagementResult result =
            await service.CloneAsync(
                new CloneWorkScheduleRequest(
                    source.Id,
                    "copy",
                    "Trùng mã"));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Mã lịch làm việc đã tồn tại.",
            result.ErrorMessage);
    }

    [Fact]
    public async Task CloneAsync_IncompleteSourceDaysFails()
    {
        var persistence =
            new TestPersistence();

        WorkSchedule source =
            CreateSchedule(
                "OFFICE",
                isActive: true);

        persistence.Schedules.Add(
            source);

        var dayRepository =
            new TestDayRepository
            {
                Days =
                [
                    new WorkScheduleDay(
                    Guid.NewGuid(),
                    source.Id,
                    DayOfWeek.Monday,
                    true,
                    new TimeOnly(
                        8,
                        0),
                    new TimeOnly(
                        17,
                        0),
                    60)
                ]
            };

        var service =
            new WorkScheduleManagementService(
                persistence,
                dayRepository);

        WorkScheduleManagementResult result =
            await service.CloneAsync(
                new CloneWorkScheduleRequest(
                    source.Id,
                    "COPY",
                    "Bản sao"));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Mẫu lịch nguồn phải có đủ 7 ngày để sao chép.",
            result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteAsync_InactiveUnusedScheduleDeletes()
    {
        var persistence =
            new TestPersistence();

        WorkSchedule schedule =
            CreateSchedule(
                "OLD",
                isActive: false);

        persistence.Schedules.Add(
            schedule);

        var service =
            new WorkScheduleManagementService(
                persistence,
                new TestDayRepository());

        WorkScheduleManagementResult result =
            await service.DeleteAsync(
                schedule.Id);

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            schedule.Id,
            result.WorkScheduleId);

        Assert.Equal(
            schedule.Id,
            persistence.DeletedWorkScheduleId);

        Assert.DoesNotContain(
            persistence.Schedules,
            item =>
                item.Id ==
                schedule.Id);
    }

    [Fact]
    public async Task DeleteAsync_ActiveScheduleFails()
    {
        var persistence =
            new TestPersistence();

        WorkSchedule schedule =
            CreateSchedule(
                "ACTIVE",
                isActive: true);

        persistence.Schedules.Add(
            schedule);

        var service =
            new WorkScheduleManagementService(
                persistence,
                new TestDayRepository());

        WorkScheduleManagementResult result =
            await service.DeleteAsync(
                schedule.Id);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Hãy ngừng sử dụng mẫu lịch trước khi xóa.",
            result.ErrorMessage);

        Assert.Null(
            persistence.DeletedWorkScheduleId);
    }

    [Fact]
    public async Task DeleteAsync_UsedScheduleFails()
    {
        var persistence =
            new TestPersistence
            {
                IsInUse =
                    true
            };

        WorkSchedule schedule =
            CreateSchedule(
                "HISTORY",
                isActive: false);

        persistence.Schedules.Add(
            schedule);

        var service =
            new WorkScheduleManagementService(
                persistence,
                new TestDayRepository());

        WorkScheduleManagementResult result =
            await service.DeleteAsync(
                schedule.Id);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Mẫu lịch đã có lịch sử sử dụng. Hãy ngừng sử dụng thay vì xóa.",
            result.ErrorMessage);

        Assert.Null(
            persistence.DeletedWorkScheduleId);
    }

    [Fact]
    public async Task DeleteAsync_MissingScheduleFails()
    {
        var persistence =
            new TestPersistence();

        var service =
            new WorkScheduleManagementService(
                persistence,
                new TestDayRepository());

        WorkScheduleManagementResult result =
            await service.DeleteAsync(
                Guid.NewGuid());

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Không tìm thấy mẫu lịch làm việc.",
            result.ErrorMessage);

        Assert.Null(
            persistence.DeletedWorkScheduleId);
    }

    private static WorkSchedule CreateSchedule(
        string code,
        bool isActive)
    {
        return new WorkSchedule(
            Guid.NewGuid(),
            code,
            $"Lịch {code}",
            "SE Asia Standard Time",
            isActive);
    }

    private sealed class TestPersistence
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

        public List<WorkScheduleDay> CreatedDays
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
            string normalized =
                code.Trim()
                    .ToUpperInvariant();

            return Task.FromResult(
                Schedules.SingleOrDefault(
                    schedule =>
                        schedule.Code ==
                        normalized));
        }

        public Task CreateAsync(
            WorkSchedule schedule,
            IReadOnlyList<WorkScheduleDay> days,
            CancellationToken cancellationToken = default)
        {
            Schedules.Add(
                schedule);

            CreatedDays.AddRange(
                days);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            WorkSchedule schedule,
            CancellationToken cancellationToken = default)
        {
            int index =
                Schedules.FindIndex(
                    existing =>
                        existing.Id ==
                        schedule.Id);

            if (index >= 0)
            {
                Schedules[index] =
                    schedule;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class TestDayRepository
        : IWorkScheduleDayRepository
    {
        public IReadOnlyList<WorkScheduleDay> Days
        {
            get;
            set;
        } = [];

        public Task<IReadOnlyList<WorkScheduleDay>>
            GetByWorkScheduleIdAsync(
                Guid workScheduleId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<WorkScheduleDay> result =
                Days
                    .Where(
                        day =>
                            day.WorkScheduleId ==
                            workScheduleId)
                    .ToArray();

            return Task.FromResult(
                result);
        }
    }
}
