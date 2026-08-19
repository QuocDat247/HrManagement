using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Schedules;

public sealed class WorkScheduleSeedService
{
    public static readonly Guid DefaultOfficeScheduleId =
        Guid.Parse(
            "17000000-0000-0000-0000-000000000001");

    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public WorkScheduleSeedService(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task SeedAsync(
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        bool alreadyExists =
            await dbContext
                .WorkSchedules
                .AnyAsync(
                    schedule =>
                        schedule.Code ==
                        "OFFICE",
                    cancellationToken);

        if (alreadyExists)
        {
            return;
        }

        var schedule =
            new WorkSchedule(
                DefaultOfficeScheduleId,
                "OFFICE",
                "Giờ hành chính",
                "SE Asia Standard Time");

        WorkScheduleDay[] days =
        [
            CreateWorkingDay(
                "17000000-0000-0000-0000-000000000101",
                DayOfWeek.Monday),

            CreateWorkingDay(
                "17000000-0000-0000-0000-000000000102",
                DayOfWeek.Tuesday),

            CreateWorkingDay(
                "17000000-0000-0000-0000-000000000103",
                DayOfWeek.Wednesday),

            CreateWorkingDay(
                "17000000-0000-0000-0000-000000000104",
                DayOfWeek.Thursday),

            CreateWorkingDay(
                "17000000-0000-0000-0000-000000000105",
                DayOfWeek.Friday),

            CreateNonWorkingDay(
                "17000000-0000-0000-0000-000000000106",
                DayOfWeek.Saturday),

            CreateNonWorkingDay(
                "17000000-0000-0000-0000-000000000107",
                DayOfWeek.Sunday)
        ];

        await dbContext.WorkSchedules.AddAsync(
            schedule,
            cancellationToken);

        await dbContext.WorkScheduleDays.AddRangeAsync(
            days,
            cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private static WorkScheduleDay CreateWorkingDay(
        string id,
        DayOfWeek dayOfWeek)
    {
        return new WorkScheduleDay(
            Guid.Parse(
                id),
            DefaultOfficeScheduleId,
            dayOfWeek,
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
    }

    private static WorkScheduleDay CreateNonWorkingDay(
        string id,
        DayOfWeek dayOfWeek)
    {
        return new WorkScheduleDay(
            Guid.Parse(
                id),
            DefaultOfficeScheduleId,
            dayOfWeek,
            isWorkingDay: false);
    }
}
