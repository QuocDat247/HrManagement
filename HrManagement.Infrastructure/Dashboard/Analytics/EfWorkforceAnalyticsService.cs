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

        List<EmploymentSnapshot> employees =
            await dbContext.Employees
                .AsNoTracking()
                .Select(employee =>
                    new EmploymentSnapshot
                    {
                        HireDate = employee.HireDate,
                        TerminationDate =
                            employee.TerminationDate,
                        Status = employee.Status
                    })
                .ToListAsync(cancellationToken);

        int employeesWithUnknownTerminationDate =
            employees.Count(employee =>
                employee.Status == EmployeeStatus.Inactive
                && employee.TerminationDate is null);

        List<EmploymentSnapshot> knownEmployees =
            employees
                .Where(employee =>
                    !(employee.Status ==
                            EmployeeStatus.Inactive
                      && employee.TerminationDate is null))
                .ToList();

        IReadOnlyList<PeriodRange> ranges =
            CreatePeriodRanges(
                year,
                grouping);

        List<WorkforceMovementPeriod> periods =
            ranges
                .Select(range =>
                    BuildPeriod(
                        range,
                        knownEmployees))
                .ToList();

        int beginningHeadcount =
            periods.Count > 0
                ? periods[0].BeginningHeadcount
                : 0;

        int endingHeadcount =
            periods.Count > 0
                ? periods[^1].EndingHeadcount
                : 0;

        int totalNewHires =
            periods.Sum(period =>
                period.NewHires);

        int totalSeparations =
            periods.Sum(period =>
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
            Periods: periods);
    }

    private static WorkforceMovementPeriod BuildPeriod(
        PeriodRange range,
        IReadOnlyList<EmploymentSnapshot> employees)
    {
        int newHires =
            employees.Count(employee =>
                employee.HireDate >= range.StartDate
                && employee.HireDate <= range.EndDate);

        int separations =
            employees.Count(employee =>
                employee.TerminationDate.HasValue
                && employee.TerminationDate.Value
                    >= range.StartDate
                && employee.TerminationDate.Value
                    <= range.EndDate);

        int beginningHeadcount =
            employees.Count(employee =>
                employee.HireDate < range.StartDate
                &&
                (
                    !employee.TerminationDate.HasValue
                    || employee.TerminationDate.Value
                        >= range.StartDate
                ));

        int endingHeadcount =
            employees.Count(employee =>
                employee.HireDate <= range.EndDate
                &&
                (
                    !employee.TerminationDate.HasValue
                    || employee.TerminationDate.Value
                        > range.EndDate
                ));

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

    private sealed class EmploymentSnapshot
    {
        public DateOnly HireDate { get; init; }

        public DateOnly? TerminationDate { get; init; }

        public EmployeeStatus Status { get; init; }
    }

    private sealed record PeriodRange(
        int PeriodNumber,
        DateOnly StartDate,
        DateOnly EndDate);
}
