using HrManagement.Application.Payroll.Periods;
using HrManagement.Domain.Payroll.Periods;
using HrManagement.Domain.Payroll.Snapshots;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Payroll.Periods;

public sealed class EfClosedPayrollQueryService
    : IClosedPayrollQueryService
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfClosedPayrollQueryService(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<ClosedPayrollReadModel?> GetAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(
            year,
            month);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        PayrollPeriod[] periods =
            await dbContext
                .PayrollPeriods
                .AsNoTracking()
                .Where(
                    period =>
                        period.Year ==
                            year
                        && period.Month ==
                            month)
                .OrderBy(
                    period =>
                        period.Id)
                .Take(
                    2)
                .ToArrayAsync(
                    cancellationToken);

        if (periods.Length == 0)
        {
            return null;
        }

        if (periods.Length > 1)
        {
            throw new InvalidOperationException(
                "Có nhiều kỳ lương cùng năm tháng. Dữ liệu cần được kiểm tra.");
        }

        PayrollPeriod period =
            periods[0];

        if (period.Status !=
                PayrollPeriodStatus.Closed
            || !period.ClosedAtUtc.HasValue
            || string.IsNullOrWhiteSpace(
                period.ClosedByUserId)
            || string.IsNullOrWhiteSpace(
                period.ClosedByUsername))
        {
            throw new InvalidOperationException(
                "Kỳ lương đã tồn tại nhưng chưa có trạng thái đóng hợp lệ.");
        }

        PayrollEmployeeSnapshot[] snapshots =
            await dbContext
                .PayrollEmployeeSnapshots
                .AsNoTracking()
                .Where(
                    snapshot =>
                        snapshot.PayrollPeriodId ==
                        period.Id)
                .OrderBy(
                    snapshot =>
                        snapshot.EmployeeCode)
                .ThenBy(
                    snapshot =>
                        snapshot.EmployeeFullName)
                .ThenBy(
                    snapshot =>
                        snapshot.EmployeeId)
                .ToArrayAsync(
                    cancellationToken);

        ClosedPayrollEmployeeItem[] employees =
            snapshots
                .Select(
                    snapshot =>
                        new ClosedPayrollEmployeeItem(
                            snapshot.Id,
                            snapshot.EmployeeId,
                            snapshot.EmployeeCode,
                            snapshot.EmployeeFullName,
                            snapshot.CurrencyCode,
                            snapshot.BaseSalaryAmount,
                            snapshot.ApprovedOvertimeMinutes,
                            snapshot.PayableOvertimeMinutes,
                            snapshot.OvertimeAmount,
                            snapshot.GrossAmount))
                .ToArray();

        ClosedPayrollCurrencySummary[] currencySummaries =
            employees
                .GroupBy(
                    employee =>
                        employee.CurrencyCode,
                    StringComparer.Ordinal)
                .OrderBy(
                    group =>
                        group.Key,
                    StringComparer.Ordinal)
                .Select(
                    group =>
                        new ClosedPayrollCurrencySummary(
                            group.Key,
                            group.Count(),
                            group.Sum(
                                employee =>
                                    employee.BaseSalaryAmount),
                            group.Sum(
                                employee =>
                                    employee.OvertimeAmount),
                            group.Sum(
                                employee =>
                                    employee.GrossAmount)))
                .ToArray();

        return new ClosedPayrollReadModel(
            period.Id,
            period.TimesheetPeriodId,
            period.Year,
            period.Month,
            period.ClosedAtUtc.Value,
            period.ClosedByUserId!,
            period.ClosedByUsername!,
            employees,
            currencySummaries);
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
                "Năm kỳ lương không hợp lệ.");
        }

        if (month < 1
            || month > 12)
        {
            throw new ArgumentOutOfRangeException(
                nameof(month),
                "Tháng kỳ lương phải từ 1 đến 12.");
        }
    }
}
