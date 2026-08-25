using HrManagement.Domain.Attendance.Timesheets;

namespace HrManagement.Application.Attendance.Timesheets;

public interface IMonthlyTimesheetQuerySource
{
    Task<TimesheetPeriod?> GetPeriodAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyTimesheetDayItem>>
        GetLiveItemsAsync(
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyTimesheetDayItem>>
        GetClosedItemsAsync(
            Guid timesheetPeriodId,
            CancellationToken cancellationToken = default);
}
