using HrManagement.Application.Workspaces.HolidayExceptions;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Workspaces.HolidayExceptions;

public sealed class EfHolidayExceptionWorkspaceQueryService
    : IHolidayExceptionWorkspaceQueryService
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfHolidayExceptionWorkspaceQueryService(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<HolidayExceptionWorkspaceSnapshot> GetAsync(
        HolidayExceptionWorkspaceQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            query);

        if (query.Year is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                "Năm lịch không hợp lệ.");
        }

        if (query.WorkScheduleId.HasValue
            && query.WorkScheduleId.Value ==
                Guid.Empty)
        {
            throw new ArgumentException(
                "Mã lịch làm việc không hợp lệ.",
                nameof(query));
        }

        DateOnly fromDate =
            new(
                query.Year,
                1,
                1);

        DateOnly toDate =
            new(
                query.Year,
                12,
                31);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        HolidayExceptionWorkspaceHolidayItem[] holidays =
            await dbContext
                .HolidayCalendarDays
                .AsNoTracking()
                .Where(
                    item =>
                        item.Date >=
                            fromDate
                        && item.Date <=
                            toDate)
                .OrderBy(
                    item =>
                        item.Date)
                .ThenBy(
                    item =>
                        item.Name)
                .Select(
                    item =>
                        new HolidayExceptionWorkspaceHolidayItem(
                            item.Id,
                            item.Date,
                            item.Name,
                            item.IsActive))
                .ToArrayAsync(
                    cancellationToken);

        HolidayExceptionWorkspaceScheduleItem[] schedules =
            await dbContext
                .WorkSchedules
                .AsNoTracking()
                .OrderByDescending(
                    schedule =>
                        schedule.IsActive)
                .ThenBy(
                    schedule =>
                        schedule.Code)
                .ThenBy(
                    schedule =>
                        schedule.Name)
                .Select(
                    schedule =>
                        new HolidayExceptionWorkspaceScheduleItem(
                            schedule.Id,
                            schedule.Code,
                            schedule.Name,
                            schedule.TimeZoneId,
                            schedule.IsActive))
                .ToArrayAsync(
                    cancellationToken);

        HolidayExceptionWorkspaceOverrideItem[] overrides =
            query.WorkScheduleId.HasValue
                ? (
                    await dbContext
                        .WorkScheduleDateOverrides
                        .AsNoTracking()
                        .Where(
                            item =>
                                item.WorkScheduleId ==
                                    query.WorkScheduleId.Value
                                && item.WorkDate >=
                                    fromDate
                                && item.WorkDate <=
                                    toDate)
                        .OrderBy(
                            item =>
                                item.WorkDate)
                        .ThenBy(
                            item =>
                                item.Id)
                        .ToArrayAsync(
                            cancellationToken)
                )
                .Select(
                    item =>
                        new HolidayExceptionWorkspaceOverrideItem(
                            item.Id,
                            item.WorkScheduleId,
                            item.WorkDate,
                            item.IsWorkingDay,
                            item.StartTime,
                            item.EndTime,
                            item.BreakMinutes,
                            item.PlannedMinutes,
                            item.IsOvernight,
                            item.Note))
                .ToArray()
                : [];

        return new HolidayExceptionWorkspaceSnapshot(
            query.Year,
            query.WorkScheduleId,
            holidays,
            schedules,
            overrides);
    }
}
