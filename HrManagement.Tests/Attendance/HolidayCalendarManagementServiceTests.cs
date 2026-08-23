using HrManagement.Application.Attendance.Calendars;
using HrManagement.Domain.Attendance.Calendars;

namespace HrManagement.Tests.Attendance;

public sealed class HolidayCalendarManagementServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesActiveHoliday()
    {
        var persistence =
            new FakePersistence();

        var service =
            new HolidayCalendarManagementService(
                persistence);

        HolidayCalendarManagementResult result =
            await service.CreateAsync(
                new CreateHolidayCalendarDayRequest(
                    new DateOnly(
                        2026,
                        9,
                        2),
                    "  Quốc khánh  "));

        Assert.True(
            result.IsSuccessful);

        Assert.NotNull(
            persistence.Created);

        Assert.Equal(
            new DateOnly(
                2026,
                9,
                2),
            persistence.Created.Date);

        Assert.Equal(
            "Quốc khánh",
            persistence.Created.Name);

        Assert.True(
            persistence.Created.IsActive);

        Assert.Equal(
            persistence.Created.Id,
            result.HolidayCalendarDayId);
    }

    [Fact]
    public async Task CreateAsync_WhenDateAlreadyExists_Fails()
    {
        var existing =
            CreateHoliday(
                isActive: false);

        var persistence =
            new FakePersistence
            {
                HolidayByDate =
                    existing
            };

        var service =
            new HolidayCalendarManagementService(
                persistence);

        HolidayCalendarManagementResult result =
            await service.CreateAsync(
                new CreateHolidayCalendarDayRequest(
                    existing.Date,
                    "Ngày khác"));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Ngày này đã tồn tại trong lịch ngày lễ.",
            result.ErrorMessage);

        Assert.Null(
            persistence.Created);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidDate_FailsWithoutPersistenceLookup()
    {
        var persistence =
            new FakePersistence();

        var service =
            new HolidayCalendarManagementService(
                persistence);

        HolidayCalendarManagementResult result =
            await service.CreateAsync(
                new CreateHolidayCalendarDayRequest(
                    default,
                    "Quốc khánh"));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            0,
            persistence.GetByDateCallCount);

        Assert.Null(
            persistence.Created);
    }

    [Fact]
    public async Task RenameAsync_WithValidRequest_UpdatesHoliday()
    {
        HolidayCalendarDay holiday =
            CreateHoliday();

        var persistence =
            new FakePersistence
            {
                HolidayById =
                    holiday
            };

        var service =
            new HolidayCalendarManagementService(
                persistence);

        HolidayCalendarManagementResult result =
            await service.RenameAsync(
                new RenameHolidayCalendarDayRequest(
                    holiday.Id,
                    "  Quốc khánh Việt Nam  "));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            "Quốc khánh Việt Nam",
            holiday.Name);

        Assert.Same(
            holiday,
            persistence.Updated);
    }

    [Fact]
    public async Task RenameAsync_WithBlankName_FailsWithoutUpdate()
    {
        HolidayCalendarDay holiday =
            CreateHoliday();

        var persistence =
            new FakePersistence
            {
                HolidayById =
                    holiday
            };

        var service =
            new HolidayCalendarManagementService(
                persistence);

        HolidayCalendarManagementResult result =
            await service.RenameAsync(
                new RenameHolidayCalendarDayRequest(
                    holiday.Id,
                    "   "));

        Assert.False(
            result.IsSuccessful);

        Assert.Null(
            persistence.Updated);

        Assert.Equal(
            "Quốc khánh",
            holiday.Name);
    }

    [Fact]
    public async Task DeactivateAsync_WithActiveHoliday_DeactivatesAndUpdates()
    {
        HolidayCalendarDay holiday =
            CreateHoliday();

        var persistence =
            new FakePersistence
            {
                HolidayById =
                    holiday
            };

        var service =
            new HolidayCalendarManagementService(
                persistence);

        HolidayCalendarManagementResult result =
            await service.DeactivateAsync(
                holiday.Id);

        Assert.True(
            result.IsSuccessful);

        Assert.False(
            holiday.IsActive);

        Assert.Same(
            holiday,
            persistence.Updated);
    }

    [Fact]
    public async Task DeactivateAsync_WhenAlreadyInactive_IsIdempotent()
    {
        HolidayCalendarDay holiday =
            CreateHoliday(
                isActive: false);

        var persistence =
            new FakePersistence
            {
                HolidayById =
                    holiday
            };

        var service =
            new HolidayCalendarManagementService(
                persistence);

        HolidayCalendarManagementResult result =
            await service.DeactivateAsync(
                holiday.Id);

        Assert.True(
            result.IsSuccessful);

        Assert.Null(
            persistence.Updated);
    }

    [Fact]
    public async Task ReactivateAsync_WithInactiveHoliday_ReactivatesAndUpdates()
    {
        HolidayCalendarDay holiday =
            CreateHoliday(
                isActive: false);

        var persistence =
            new FakePersistence
            {
                HolidayById =
                    holiday
            };

        var service =
            new HolidayCalendarManagementService(
                persistence);

        HolidayCalendarManagementResult result =
            await service.ReactivateAsync(
                holiday.Id);

        Assert.True(
            result.IsSuccessful);

        Assert.True(
            holiday.IsActive);

        Assert.Same(
            holiday,
            persistence.Updated);
    }

    [Fact]
    public async Task ReactivateAsync_WhenAlreadyActive_IsIdempotent()
    {
        HolidayCalendarDay holiday =
            CreateHoliday();

        var persistence =
            new FakePersistence
            {
                HolidayById =
                    holiday
            };

        var service =
            new HolidayCalendarManagementService(
                persistence);

        HolidayCalendarManagementResult result =
            await service.ReactivateAsync(
                holiday.Id);

        Assert.True(
            result.IsSuccessful);

        Assert.Null(
            persistence.Updated);
    }

    private static HolidayCalendarDay CreateHoliday(
        bool isActive = true)
    {
        return new HolidayCalendarDay(
            Guid.NewGuid(),
            new DateOnly(
                2026,
                9,
                2),
            "Quốc khánh",
            isActive);
    }

    private sealed class FakePersistence
        : IHolidayCalendarManagementPersistence
    {
        public HolidayCalendarDay? HolidayById
        {
            get;
            init;
        }

        public HolidayCalendarDay? HolidayByDate
        {
            get;
            init;
        }

        public HolidayCalendarDay? Created
        {
            get;
            private set;
        }

        public HolidayCalendarDay? Updated
        {
            get;
            private set;
        }

        public int GetByDateCallCount
        {
            get;
            private set;
        }

        public Task<HolidayCalendarDay?> GetByIdAsync(
            Guid holidayCalendarDayId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                HolidayById);
        }

        public Task<HolidayCalendarDay?> GetByDateAsync(
            DateOnly date,
            CancellationToken cancellationToken = default)
        {
            GetByDateCallCount++;

            return Task.FromResult(
                HolidayByDate);
        }

        public Task CreateAsync(
            HolidayCalendarDay holiday,
            CancellationToken cancellationToken = default)
        {
            Created =
                holiday;

            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            HolidayCalendarDay holiday,
            CancellationToken cancellationToken = default)
        {
            Updated =
                holiday;

            return Task.CompletedTask;
        }
    }
}
