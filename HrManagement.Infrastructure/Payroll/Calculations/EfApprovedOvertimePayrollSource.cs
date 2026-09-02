using HrManagement.Application.Payroll.Calculations;
using HrManagement.Domain.Overtime.Requests;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Payroll.Calculations;

public sealed class EfApprovedOvertimePayrollSource
    : IApprovedOvertimePayrollSource
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfApprovedOvertimePayrollSource(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<IReadOnlyList<ApprovedOvertimePayrollItem>>
        GetApprovedAsync(
            IReadOnlyCollection<Guid> employeeIds,
            DateOnly periodStart,
            DateOnly periodEnd,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            employeeIds);

        if (periodStart == default)
        {
            throw new ArgumentException(
                "Ngày bắt đầu kỳ lương không hợp lệ.",
                nameof(periodStart));
        }

        if (periodEnd == default)
        {
            throw new ArgumentException(
                "Ngày kết thúc kỳ lương không hợp lệ.",
                nameof(periodEnd));
        }

        if (periodEnd <
            periodStart)
        {
            throw new ArgumentException(
                "Ngày kết thúc kỳ lương không thể trước ngày bắt đầu.",
                nameof(periodEnd));
        }

        Guid[] normalizedEmployeeIds =
            employeeIds
                .Distinct()
                .ToArray();

        if (normalizedEmployeeIds.Any(
                employeeId =>
                    employeeId == Guid.Empty))
        {
            throw new ArgumentException(
                "Danh sách nhân viên chứa mã không hợp lệ.",
                nameof(employeeIds));
        }

        if (normalizedEmployeeIds.Length == 0)
        {
            return [];
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        var rows =
            await dbContext
                .OvertimeRequests
                .AsNoTracking()
                .Where(
                    request =>
                        normalizedEmployeeIds.Contains(
                            request.EmployeeId)
                        && request.WorkDate >=
                            periodStart
                        && request.WorkDate <=
                            periodEnd
                        && request.Status ==
                            OvertimeRequestStatus.Approved)
                .OrderBy(
                    request =>
                        request.EmployeeId)
                .ThenBy(
                    request =>
                        request.WorkDate)
                .ThenBy(
                    request =>
                        request.Id)
                .Select(
                    request =>
                        new
                        {
                            request.Id,
                            request.EmployeeId,
                            request.WorkDate,
                            request.RequestedMinutes,
                            request.ApprovedMinutes
                        })
                .ToArrayAsync(
                    cancellationToken);

        foreach (var row in rows)
        {
            if (!row.ApprovedMinutes.HasValue
                || row.ApprovedMinutes.Value <= 0
                || row.ApprovedMinutes.Value >
                    row.RequestedMinutes)
            {
                throw new InvalidOperationException(
                    $"Yêu cầu tăng ca đã duyệt {row.Id} có số phút được duyệt không hợp lệ.");
            }
        }

        return rows
            .Select(
                row =>
                    new ApprovedOvertimePayrollItem(
                        row.Id,
                        row.EmployeeId,
                        row.WorkDate,
                        row.ApprovedMinutes!.Value))
            .ToArray();
    }
}
