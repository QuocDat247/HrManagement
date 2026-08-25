namespace HrManagement.Application.Attendance.Timesheets;

public interface IMonthlyTimesheetQueryService
{
    Task<MonthlyTimesheetReadModel> GetAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);
}
