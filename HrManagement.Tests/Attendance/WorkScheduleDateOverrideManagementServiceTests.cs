using HrManagement.Application.Attendance.Schedules;
using HrManagement.Application.Attendance.Schedules.Overrides;
using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Tests.Attendance;

public sealed class WorkScheduleDateOverrideManagementServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesOverride()
    {
        WorkSchedule schedule =
            CreateSchedule(
                isActive: true);

        var schedules =
            new FakeWorkScheduleRepository
            {
                Schedule =
                    schedule
            };

        var persistence =
            new FakePersistence();

        var service =
            new WorkScheduleDateOverrideManagementService(
                schedules,
                persistence);

        WorkScheduleDateOverrideManagementResult result =
            await service.CreateAsync(
                new CreateWorkScheduleDateOverrideRequest(
                    schedule.Id,
                    new DateOnly(
                        2026,
                        9,
                        5),
                    true,
                    new TimeOnly(
                        8,
                        0),
                    new TimeOnly(
                        17,
                        0),
                    60,
                    "  Làm bù  "));

        Assert.True(
            result.IsSuccessful);

        Assert.NotNull(
            persistence.Created);

        Assert.Equal(
            schedule.Id,
            persistence.Created.WorkScheduleId);

        Assert.Equal(
            new DateOnly(
                2026,
                9,
                5),
            persistence.Created.WorkDate);

        Assert.True(
            persistence.Created.IsWorkingDay);

        Assert.Equal(
            480,
            persistence.Created.PlannedMinutes);

        Assert.Equal(
            "Làm bù",
            persistence.Created.Note);

        Assert.Equal(
            persistence.Created.Id,
            result.WorkScheduleDateOverrideId);
    }

    [Fact]
    public async Task CreateAsync_WithInactiveSchedule_IsAllowed()
    {
        WorkSchedule schedule =
            CreateSchedule(
                isActive: false);

        var schedules =
            new FakeWorkScheduleRepository
            {
                Schedule =
                    schedule
            };

        var persistence =
            new FakePersistence();

        var service =
            new WorkScheduleDateOverrideManagementService(
                schedules,
                persistence);

        WorkScheduleDateOverrideManagementResult result =
            await service.CreateAsync(
                new CreateWorkScheduleDateOverrideRequest(
                    schedule.Id,
                    new DateOnly(
                        2026,
                        9,
                        5),
                    false,
                    Note:
                        "Nghỉ đặc biệt"));

        Assert.True(
            result.IsSuccessful);

        Assert.NotNull(
            persistence.Created);
    }

    [Fact]
    public async Task CreateAsync_WhenScheduleDoesNotExist_Fails()
    {
        var schedules =
            new FakeWorkScheduleRepository();

        var persistence =
            new FakePersistence();

        var service =
            new WorkScheduleDateOverrideManagementService(
                schedules,
                persistence);

        WorkScheduleDateOverrideManagementResult result =
            await service.CreateAsync(
                new CreateWorkScheduleDateOverrideRequest(
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        9,
                        5),
                    false));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Không tìm thấy lịch làm việc.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            persistence.GetByScheduleAndDateCallCount);

        Assert.Null(
            persistence.Created);
    }

    [Fact]
    public async Task CreateAsync_WhenOverrideAlreadyExists_Fails()
    {
        WorkSchedule schedule =
            CreateSchedule();

        var existing =
            new WorkScheduleDateOverride(
                Guid.NewGuid(),
                schedule.Id,
                new DateOnly(
                    2026,
                    9,
                    5),
                false);

        var schedules =
            new FakeWorkScheduleRepository
            {
                Schedule =
                    schedule
            };

        var persistence =
            new FakePersistence
            {
                ExistingByScheduleAndDate =
                    existing
            };

        var service =
            new WorkScheduleDateOverrideManagementService(
                schedules,
                persistence);

        WorkScheduleDateOverrideManagementResult result =
            await service.CreateAsync(
                new CreateWorkScheduleDateOverrideRequest(
                    schedule.Id,
                    existing.WorkDate,
                    true,
                    new TimeOnly(
                        8,
                        0),
                    new TimeOnly(
                        17,
                        0),
                    60));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Lịch làm việc đã có ngoại lệ cho ngày này.",
            result.ErrorMessage);

        Assert.Null(
            persistence.Created);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidExpectation_FailsBeforePersistenceLookup()
    {
        var schedules =
            new FakeWorkScheduleRepository();

        var persistence =
            new FakePersistence();

        var service =
            new WorkScheduleDateOverrideManagementService(
                schedules,
                persistence);

        WorkScheduleDateOverrideManagementResult result =
            await service.CreateAsync(
                new CreateWorkScheduleDateOverrideRequest(
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        9,
                        5),
                    true));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            0,
            schedules.GetByIdCallCount);

        Assert.Equal(
            0,
            persistence.GetByScheduleAndDateCallCount);

        Assert.Null(
            persistence.Created);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_PreservesIdentityAndDate()
    {
        WorkScheduleDateOverride existing =
            CreateOverride();

        var schedules =
            new FakeWorkScheduleRepository();

        var persistence =
            new FakePersistence
            {
                ExistingById =
                    existing
            };

        var service =
            new WorkScheduleDateOverrideManagementService(
                schedules,
                persistence);

        WorkScheduleDateOverrideManagementResult result =
            await service.UpdateAsync(
                new UpdateWorkScheduleDateOverrideRequest(
                    existing.Id,
                    true,
                    new TimeOnly(
                        22,
                        0),
                    new TimeOnly(
                        6,
                        0),
                    30,
                    "  Trực ngày lễ  "));

        Assert.True(
            result.IsSuccessful);

        Assert.NotNull(
            persistence.Updated);

        Assert.Equal(
            existing.Id,
            persistence.Updated.Id);

        Assert.Equal(
            existing.WorkScheduleId,
            persistence.Updated.WorkScheduleId);

        Assert.Equal(
            existing.WorkDate,
            persistence.Updated.WorkDate);

        Assert.True(
            persistence.Updated.IsOvernight);

        Assert.Equal(
            450,
            persistence.Updated.PlannedMinutes);

        Assert.Equal(
            "Trực ngày lễ",
            persistence.Updated.Note);
    }

    [Fact]
    public async Task UpdateAsync_WhenOverrideDoesNotExist_Fails()
    {
        var persistence =
            new FakePersistence();

        var service =
            new WorkScheduleDateOverrideManagementService(
                new FakeWorkScheduleRepository(),
                persistence);

        WorkScheduleDateOverrideManagementResult result =
            await service.UpdateAsync(
                new UpdateWorkScheduleDateOverrideRequest(
                    Guid.NewGuid(),
                    false));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Không tìm thấy ngoại lệ lịch làm việc.",
            result.ErrorMessage);

        Assert.Null(
            persistence.Updated);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidExpectation_FailsWithoutUpdate()
    {
        WorkScheduleDateOverride existing =
            CreateOverride();

        var persistence =
            new FakePersistence
            {
                ExistingById =
                    existing
            };

        var service =
            new WorkScheduleDateOverrideManagementService(
                new FakeWorkScheduleRepository(),
                persistence);

        WorkScheduleDateOverrideManagementResult result =
            await service.UpdateAsync(
                new UpdateWorkScheduleDateOverrideRequest(
                    existing.Id,
                    false,
                    new TimeOnly(
                        8,
                        0),
                    new TimeOnly(
                        17,
                        0)));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            persistence.Updated);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingOverride_Deletes()
    {
        WorkScheduleDateOverride existing =
            CreateOverride();

        var persistence =
            new FakePersistence
            {
                ExistingById =
                    existing
            };

        var service =
            new WorkScheduleDateOverrideManagementService(
                new FakeWorkScheduleRepository(),
                persistence);

        WorkScheduleDateOverrideManagementResult result =
            await service.DeleteAsync(
                existing.Id);

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            existing.Id,
            persistence.DeletedId);
    }

    [Fact]
    public async Task DeleteAsync_WhenOverrideDoesNotExist_Fails()
    {
        var persistence =
            new FakePersistence();

        var service =
            new WorkScheduleDateOverrideManagementService(
                new FakeWorkScheduleRepository(),
                persistence);

        WorkScheduleDateOverrideManagementResult result =
            await service.DeleteAsync(
                Guid.NewGuid());

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Không tìm thấy ngoại lệ lịch làm việc.",
            result.ErrorMessage);

        Assert.Null(
            persistence.DeletedId);
    }

    private static WorkSchedule CreateSchedule(
        bool isActive = true)
    {
        return new WorkSchedule(
            Guid.NewGuid(),
            "TEST",
            "Lịch thử nghiệm",
            "SE Asia Standard Time",
            isActive);
    }

    private static WorkScheduleDateOverride CreateOverride()
    {
        return new WorkScheduleDateOverride(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(
                2026,
                9,
                5),
            false,
            note:
                "Nghỉ điều chỉnh");
    }

    private sealed class FakeWorkScheduleRepository
        : IWorkScheduleRepository
    {
        public WorkSchedule? Schedule
        {
            get;
            init;
        }

        public int GetByIdCallCount
        {
            get;
            private set;
        }

        public Task<WorkSchedule?> GetByIdAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;

            return Task.FromResult(
                Schedule);
        }
    }

    private sealed class FakePersistence
        : IWorkScheduleDateOverrideManagementPersistence
    {
        public WorkScheduleDateOverride? ExistingById
        {
            get;
            init;
        }

        public WorkScheduleDateOverride? ExistingByScheduleAndDate
        {
            get;
            init;
        }

        public WorkScheduleDateOverride? Created
        {
            get;
            private set;
        }

        public WorkScheduleDateOverride? Updated
        {
            get;
            private set;
        }

        public Guid? DeletedId
        {
            get;
            private set;
        }

        public int GetByScheduleAndDateCallCount
        {
            get;
            private set;
        }

        public Task<WorkScheduleDateOverride?> GetByIdAsync(
            Guid workScheduleDateOverrideId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                ExistingById);
        }

        public Task<WorkScheduleDateOverride?> GetByScheduleAndDateAsync(
            Guid workScheduleId,
            DateOnly workDate,
            CancellationToken cancellationToken = default)
        {
            GetByScheduleAndDateCallCount++;

            return Task.FromResult(
                ExistingByScheduleAndDate);
        }

        public Task CreateAsync(
            WorkScheduleDateOverride item,
            CancellationToken cancellationToken = default)
        {
            Created =
                item;

            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            WorkScheduleDateOverride item,
            CancellationToken cancellationToken = default)
        {
            Updated =
                item;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(
            Guid workScheduleDateOverrideId,
            CancellationToken cancellationToken = default)
        {
            DeletedId =
                workScheduleDateOverrideId;

            return Task.CompletedTask;
        }
    }
}
