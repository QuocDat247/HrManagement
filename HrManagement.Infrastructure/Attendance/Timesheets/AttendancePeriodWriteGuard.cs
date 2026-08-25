using HrManagement.Domain.Attendance.Timesheets;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Timesheets;

internal static class AttendancePeriodWriteGuard
{
    public static async Task EnsureUnlockedAsync(
        HrManagementDbContext dbContext,
        DateOnly workDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            dbContext);

        if (workDate == default)
        {
            throw new ArgumentException(
                "Ngày chấm công không hợp lệ.",
                nameof(workDate));
        }

        bool isClosed =
            await dbContext
                .TimesheetPeriods
                .AsNoTracking()
                .AnyAsync(
                    period =>
                        period.Year == workDate.Year
                        && period.Month == workDate.Month
                        && period.Status ==
                            TimesheetPeriodStatus.Closed,
                    cancellationToken);

        if (isClosed)
        {
            throw new InvalidOperationException(
                "Kỳ công của ngày chấm công đã được đóng. Không thể thay đổi dữ liệu chấm công.");
        }
    }
}
