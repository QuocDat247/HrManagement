using HrManagement.Domain.Attendance.Calendars;
using HrManagement.Domain.Attendance.Expectations;
using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Application.Attendance.Expectations;

public sealed class WorkExpectationResolver
    : IWorkExpectationResolver
{
    private readonly IWorkExpectationResolutionPersistence
        _persistence;

    public WorkExpectationResolver(
        IWorkExpectationResolutionPersistence persistence)
    {
        _persistence =
            persistence;
    }

    public async Task<ResolvedWorkExpectation?> ResolveAsync(
        Guid workScheduleId,
        DateOnly workDate,
        CancellationToken cancellationToken = default)
    {
        ValidateWorkScheduleId(
            workScheduleId);

        ValidateWorkDate(
            workDate);

        IReadOnlyDictionary<Guid, ResolvedWorkExpectation>
            resolved =
                await ResolveManyAsync(
                    new[]
                    {
                        workScheduleId
                    },
                    workDate,
                    cancellationToken);

        return resolved.TryGetValue(
            workScheduleId,
            out ResolvedWorkExpectation? expectation)
                ? expectation
                : null;
    }

    public async Task<IReadOnlyDictionary<Guid, ResolvedWorkExpectation>>
        ResolveManyAsync(
            IReadOnlyCollection<Guid> workScheduleIds,
            DateOnly workDate,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            workScheduleIds);

        ValidateWorkDate(
            workDate);

        if (workScheduleIds.Count == 0)
        {
            return new Dictionary<Guid, ResolvedWorkExpectation>();
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

        Guid[] uniqueScheduleIds =
            workScheduleIds
                .Distinct()
                .ToArray();

        WorkExpectationResolutionData data =
            await _persistence
                .LoadAsync(
                    workDate,
                    uniqueScheduleIds,
                    cancellationToken);

        HolidayCalendarDay? activeHoliday =
            data.Holiday is
            {
                IsActive: true
            }
            && data.Holiday.Date ==
                workDate
                ? data.Holiday
                : null;

        var result =
            new Dictionary<Guid, ResolvedWorkExpectation>();

        foreach (Guid workScheduleId in uniqueScheduleIds)
        {
            WorkScheduleDateOverride? dateOverride =
                FindDateOverride(
                    data.DateOverrides,
                    workScheduleId,
                    workDate);

            if (dateOverride is not null)
            {
                result[workScheduleId] =
                    FromDateOverride(
                        dateOverride);

                continue;
            }

            if (activeHoliday is not null)
            {
                result[workScheduleId] =
                    FromHoliday(
                        workScheduleId,
                        workDate,
                        activeHoliday);

                continue;
            }

            WorkScheduleDay? weeklyDay =
                FindWeeklyDay(
                    data.WeeklyDays,
                    workScheduleId,
                    workDate.DayOfWeek);

            if (weeklyDay is null)
            {
                continue;
            }

            result[workScheduleId] =
                FromWeeklyDay(
                    weeklyDay,
                    workDate);
        }

        return result;
    }

    private static WorkScheduleDateOverride? FindDateOverride(
    IReadOnlyList<WorkScheduleDateOverride> dateOverrides,
    Guid workScheduleId,
    DateOnly workDate)
    {
        WorkScheduleDateOverride[] matches =
            dateOverrides
                .Where(
                    item =>
                        item.WorkScheduleId ==
                        workScheduleId
                        && item.WorkDate ==
                        workDate)
                .Take(
                    2)
                .ToArray();

        if (matches.Length > 1)
        {
            throw new InvalidOperationException(
                "Phát hiện nhiều ngoại lệ lịch làm việc cho cùng lịch và ngày.");
        }

        return matches.SingleOrDefault();
    }

    private static WorkScheduleDay? FindWeeklyDay(
        IReadOnlyList<WorkScheduleDay> weeklyDays,
        Guid workScheduleId,
        DayOfWeek dayOfWeek)
    {
        WorkScheduleDay[] matches =
            weeklyDays
                .Where(
                    day =>
                        day.WorkScheduleId ==
                        workScheduleId
                        && day.DayOfWeek ==
                        dayOfWeek)
                .Take(
                    2)
                .ToArray();

        if (matches.Length > 1)
        {
            throw new InvalidOperationException(
                "Phát hiện nhiều cấu hình ngày trong tuần cho cùng lịch làm việc.");
        }

        return matches.SingleOrDefault();
    }

    private static ResolvedWorkExpectation FromDateOverride(
        WorkScheduleDateOverride item)
    {
        return new ResolvedWorkExpectation(
            item.WorkScheduleId,
            item.WorkDate,
            item.IsWorkingDay,
            item.StartTime,
            item.EndTime,
            item.BreakMinutes,
            item.PlannedMinutes,
            item.IsOvernight,
            WorkExpectationSource.DateOverride,
            item.Id,
            item.Note);
    }

    private static ResolvedWorkExpectation FromHoliday(
        Guid workScheduleId,
        DateOnly workDate,
        HolidayCalendarDay holiday)
    {
        return new ResolvedWorkExpectation(
            workScheduleId,
            workDate,
            false,
            null,
            null,
            0,
            0,
            false,
            WorkExpectationSource.Holiday,
            holiday.Id,
            holiday.Name);
    }

    private static ResolvedWorkExpectation FromWeeklyDay(
        WorkScheduleDay day,
        DateOnly workDate)
    {
        return new ResolvedWorkExpectation(
            day.WorkScheduleId,
            workDate,
            day.IsWorkingDay,
            day.StartTime,
            day.EndTime,
            day.BreakMinutes,
            day.PlannedMinutes,
            day.IsOvernight,
            WorkExpectationSource.WeeklySchedule,
            day.Id,
            null);
    }

    private static void ValidateWorkScheduleId(
        Guid workScheduleId)
    {
        if (workScheduleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã lịch làm việc không hợp lệ.",
                nameof(workScheduleId));
        }
    }

    private static void ValidateWorkDate(
        DateOnly workDate)
    {
        if (workDate == default)
        {
            throw new ArgumentException(
                "Ngày làm việc không hợp lệ.",
                nameof(workDate));
        }
    }
}
