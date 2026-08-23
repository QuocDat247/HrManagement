using HrManagement.Application.Attendance.Calendars;
using HrManagement.Domain.Attendance.Calendars;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Calendars;

public sealed class EfHolidayCalendarManagementPersistence
    : IHolidayCalendarManagementPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfHolidayCalendarManagementPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<HolidayCalendarDay?> GetByIdAsync(
        Guid holidayCalendarDayId,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .HolidayCalendarDays
            .AsNoTracking()
            .SingleOrDefaultAsync(
                holiday =>
                    holiday.Id ==
                    holidayCalendarDayId,
                cancellationToken);
    }

    public async Task<HolidayCalendarDay?> GetByDateAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .HolidayCalendarDays
            .AsNoTracking()
            .SingleOrDefaultAsync(
                holiday =>
                    holiday.Date ==
                    date,
                cancellationToken);
    }

    public async Task CreateAsync(
        HolidayCalendarDay holiday,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            holiday);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await dbContext
            .HolidayCalendarDays
            .AddAsync(
                holiday,
                cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task UpdateAsync(
        HolidayCalendarDay holiday,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            holiday);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        dbContext
            .HolidayCalendarDays
            .Update(
                holiday);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
