using HrManagement.Application.Attendance.Expectations;
using HrManagement.Domain.Attendance.Calendars;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Expectations;

public sealed class EfWorkExpectationResolutionPersistence
    : IWorkExpectationResolutionPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfWorkExpectationResolutionPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<WorkExpectationResolutionData> LoadAsync(
        DateOnly workDate,
        IReadOnlyCollection<Guid> workScheduleIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            workScheduleIds);

        if (workDate == default)
        {
            throw new ArgumentException(
                "Ngày làm việc không hợp lệ.",
                nameof(workDate));
        }

        if (workScheduleIds.Any(
                workScheduleId =>
                    workScheduleId ==
                    Guid.Empty))
        {
            throw new ArgumentException(
                "Danh sách lịch làm việc chứa mã không hợp lệ.",
                nameof(workScheduleIds));
        }

        Guid[] ids =
            workScheduleIds
                .Distinct()
                .ToArray();

        if (ids.Length == 0)
        {
            return new WorkExpectationResolutionData(
                null,
                Array.Empty<WorkScheduleDay>(),
                Array.Empty<WorkScheduleDateOverride>());
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        HolidayCalendarDay? holiday =
            await dbContext
                .HolidayCalendarDays
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Date ==
                        workDate,
                    cancellationToken);

        DayOfWeek dayOfWeek =
            workDate.DayOfWeek;

        WorkScheduleDay[] weeklyDays =
            await dbContext
                .WorkScheduleDays
                .AsNoTracking()
                .Where(
                    day =>
                        ids.Contains(
                            day.WorkScheduleId)
                        && day.DayOfWeek ==
                            dayOfWeek)
                .OrderBy(
                    day =>
                        day.WorkScheduleId)
                .ThenBy(
                    day =>
                        day.Id)
                .ToArrayAsync(
                    cancellationToken);

        WorkScheduleDateOverride[] dateOverrides =
            await dbContext
                .WorkScheduleDateOverrides
                .AsNoTracking()
                .Where(
                    item =>
                        ids.Contains(
                            item.WorkScheduleId)
                        && item.WorkDate ==
                            workDate)
                .OrderBy(
                    item =>
                        item.WorkScheduleId)
                .ThenBy(
                    item =>
                        item.Id)
                .ToArrayAsync(
                    cancellationToken);

        return new WorkExpectationResolutionData(
            holiday,
            weeklyDays,
            dateOverrides);
    }
}
