using HrManagement.Domain.Attendance.Timesheets;

namespace HrManagement.Application.Attendance.Timesheets;

public sealed class MonthlyTimesheetQueryService
    : IMonthlyTimesheetQueryService
{
    private readonly IMonthlyTimesheetQuerySource
        _querySource;

    public MonthlyTimesheetQueryService(
        IMonthlyTimesheetQuerySource querySource)
    {
        _querySource =
            querySource;
    }

    public async Task<MonthlyTimesheetReadModel> GetAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(
            year,
            month);

        TimesheetPeriod? period =
            await _querySource.GetPeriodAsync(
                year,
                month,
                cancellationToken);

        if (period is not null
            && period.IsClosed)
        {
            IReadOnlyList<MonthlyTimesheetDayItem>
                closedItems =
                    await _querySource
                        .GetClosedItemsAsync(
                            period.Id,
                            cancellationToken);

            return new MonthlyTimesheetReadModel(
                year,
                month,
                period.Id,
                TimesheetPeriodStatus.Closed,
                closedItems);
        }

        DateOnly startDate =
            new(
                year,
                month,
                1);

        DateOnly endDate =
            new(
                year,
                month,
                DateTime.DaysInMonth(
                    year,
                    month));

        IReadOnlyList<MonthlyTimesheetDayItem>
            liveItems =
                await _querySource
                    .GetLiveItemsAsync(
                        startDate,
                        endDate,
                        cancellationToken);

        return new MonthlyTimesheetReadModel(
            year,
            month,
            period?.Id,
            TimesheetPeriodStatus.Open,
            liveItems);
    }

    private static void ValidatePeriod(
        int year,
        int month)
    {
        if (year < 2000
            || year > 9999)
        {
            throw new ArgumentOutOfRangeException(
                nameof(year),
                "Năm kỳ công không hợp lệ.");
        }

        if (month < 1
            || month > 12)
        {
            throw new ArgumentOutOfRangeException(
                nameof(month),
                "Tháng kỳ công phải từ 1 đến 12.");
        }
    }
}
