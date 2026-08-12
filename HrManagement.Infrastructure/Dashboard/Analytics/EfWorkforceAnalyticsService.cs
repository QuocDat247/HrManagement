using HrManagement.Application.Dashboard.Analytics;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Dashboard.Analytics;

public sealed class EfWorkforceAnalyticsService
    : IWorkforceAnalyticsService
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfWorkforceAnalyticsService(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<WorkforceMovementSummary>
        GetWorkforceMovementAsync(
            int year,
            WorkforceAnalyticsGrouping grouping =
                WorkforceAnalyticsGrouping.Monthly,
            CancellationToken cancellationToken = default)
    {
        if (year is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(
                nameof(year));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        // 1. Tải EmploymentPeriods
        List<EmploymentPeriodSnapshot> periods =
            await dbContext.EmploymentPeriods
                .AsNoTracking()
                .Select(period =>
                    new EmploymentPeriodSnapshot(
                        period.EmployeeId,
                        period.StartDate,
                        period.EndDate))
                .ToListAsync(cancellationToken);

        // Legacy warning vẫn lấy từ Employees
        int employeesWithUnknownTerminationDate =
            await dbContext.Employees
                .AsNoTracking()
                .CountAsync(
                    employee =>
                        employee.Status ==
                            EmployeeStatus.Inactive
                        && employee.TerminationDate == null,
                    cancellationToken);

        IReadOnlyList<PeriodRange> ranges =
            CreatePeriodRanges(
                year,
                grouping);

        // 3. Gọi BuildPeriod với periods
        List<WorkforceMovementPeriod> movementPeriods =
            ranges
                .Select(range =>
                    BuildPeriod(
                        range,
                        periods))
                .ToList();

        // Summary logic giữ nguyên
        int beginningHeadcount =
            movementPeriods.Count > 0
                ? movementPeriods[0].BeginningHeadcount
                : 0;
        int endingHeadcount =
            movementPeriods.Count > 0
                ? movementPeriods[^1].EndingHeadcount
                : 0;
        int totalNewHires =
            movementPeriods.Sum(period =>
                period.NewHires);
        int totalSeparations =
            movementPeriods.Sum(period =>
                period.Separations);
        int netChange =
            totalNewHires - totalSeparations;
        decimal averageHeadcount =
            (beginningHeadcount + endingHeadcount)
            / 2m;
        decimal turnoverRate =
            CalculateTurnoverRate(
                totalSeparations,
                averageHeadcount);

        return new WorkforceMovementSummary(
            Year: year,
            Grouping: grouping,
            BeginningHeadcount: beginningHeadcount,
            EndingHeadcount: endingHeadcount,
            TotalNewHires: totalNewHires,
            TotalSeparations: totalSeparations,
            NetChange: netChange,
            AverageHeadcount: averageHeadcount,
            TurnoverRate: turnoverRate,
            EmployeesWithUnknownTerminationDate:
                employeesWithUnknownTerminationDate,
            Periods: movementPeriods);
    }

    // 2. BuildPeriod mới
    private static WorkforceMovementPeriod BuildPeriod(
        PeriodRange range,
        IReadOnlyList<EmploymentPeriodSnapshot> periods)
    {
        int newHires =
            periods.Count(period =>
                period.StartDate >= range.StartDate
                && period.StartDate <= range.EndDate);

        int separations =
            periods.Count(period =>
                period.EndDate.HasValue
                && period.EndDate.Value
                    >= range.StartDate
                && period.EndDate.Value
                    <= range.EndDate);

        int beginningHeadcount =
            periods
                .Where(period =>
                    period.StartDate < range.StartDate
                    &&
                    (
                        !period.EndDate.HasValue
                        || period.EndDate.Value
                            >= range.StartDate
                    ))
                .Select(period => period.EmployeeId)
                .Distinct()
                .Count();

        int endingHeadcount =
            periods
                .Where(period =>
                    period.StartDate <= range.EndDate
                    &&
                    (
                        !period.EndDate.HasValue
                        || period.EndDate.Value
                            > range.EndDate
                    ))
                .Select(period => period.EmployeeId)
                .Distinct()
                .Count();

        decimal averageHeadcount =
            (beginningHeadcount + endingHeadcount)
            / 2m;

        decimal turnoverRate =
            CalculateTurnoverRate(
                separations,
                averageHeadcount);

        return new WorkforceMovementPeriod(
            PeriodNumber: range.PeriodNumber,
            StartDate: range.StartDate,
            EndDate: range.EndDate,
            NewHires: newHires,
            Separations: separations,
            BeginningHeadcount: beginningHeadcount,
            EndingHeadcount: endingHeadcount,
            AverageHeadcount: averageHeadcount,
            TurnoverRate: turnoverRate,
            NetChange: newHires - separations);
    }

    private static decimal CalculateTurnoverRate(
        int separations,
        decimal averageHeadcount)
    {
        if (averageHeadcount == 0)
        {
            return 0;
        }
        return Math.Round(
            separations / averageHeadcount * 100m,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static IReadOnlyList<PeriodRange>
        CreatePeriodRanges(
            int year,
            WorkforceAnalyticsGrouping grouping)
    {
        return grouping switch
        {
            WorkforceAnalyticsGrouping.Monthly =>
                CreateMonthlyRanges(year),
            WorkforceAnalyticsGrouping.Quarterly =>
                CreateQuarterlyRanges(year),
            _ => throw new ArgumentOutOfRangeException(
                nameof(grouping))
        };
    }

    private static IReadOnlyList<PeriodRange>
        CreateMonthlyRanges(int year)
    {
        var ranges =
            new List<PeriodRange>(12);
        for (int month = 1; month <= 12; month++)
        {
            DateOnly startDate =
                new(year, month, 1);
            DateOnly endDate =
                new(
                    year,
                    month,
                    DateTime.DaysInMonth(
                        year,
                        month));
            ranges.Add(
                new PeriodRange(
                    month,
                    startDate,
                    endDate));
        }
        return ranges;
    }

    private static IReadOnlyList<PeriodRange>
        CreateQuarterlyRanges(int year)
    {
        var ranges =
            new List<PeriodRange>(4);
        for (int quarter = 1;
             quarter <= 4;
             quarter++)
        {
            int startMonth =
                (quarter - 1) * 3 + 1;
            int endMonth =
                startMonth + 2;
            DateOnly startDate =
                new(
                    year,
                    startMonth,
                    1);
            DateOnly endDate =
                new(
                    year,
                    endMonth,
                    DateTime.DaysInMonth(
                        year,
                        endMonth));
            ranges.Add(
                new PeriodRange(
                    quarter,
                    startDate,
                    endDate));
        }
        return ranges;
    }

    // Snapshot mới
    private sealed record EmploymentPeriodSnapshot(
        Guid EmployeeId,
        DateOnly StartDate,
        DateOnly? EndDate);

    private sealed record PeriodRange(
        int PeriodNumber,
        DateOnly StartDate,
        DateOnly EndDate);
}
