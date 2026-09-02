using HrManagement.Application.Payroll.Periods;
using HrManagement.Domain.Payroll.Periods;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Payroll.Periods;

public sealed class EfPayrollFinancialPeriodLockSource
    : IPayrollFinancialPeriodLockSource
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfPayrollFinancialPeriodLockSource(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<bool> IsLockedAsync(
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null,
        CancellationToken cancellationToken = default)
    {
        if (effectiveFrom == default)
        {
            throw new ArgumentException(
                "Ngày bắt đầu kiểm tra khóa không hợp lệ.",
                nameof(effectiveFrom));
        }

        if (effectiveTo.HasValue
            && effectiveTo.Value <
                effectiveFrom)
        {
            throw new ArgumentException(
                "Ngày kết thúc kiểm tra khóa không thể trước ngày bắt đầu.",
                nameof(effectiveTo));
        }

        int fromPeriodKey =
            effectiveFrom.Year * 100
            + effectiveFrom.Month;

        int? toPeriodKey =
            effectiveTo.HasValue
                ? effectiveTo.Value.Year * 100
                    + effectiveTo.Value.Month
                : null;

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .PayrollPeriods
            .AsNoTracking()
            .AnyAsync(
                period =>
                    period.Status ==
                        PayrollPeriodStatus.Closed
                    && (
                        period.Year * 100
                        + period.Month
                    ) >= fromPeriodKey
                    && (
                        !toPeriodKey.HasValue
                        || (
                            period.Year * 100
                            + period.Month
                        ) <= toPeriodKey.Value
                    ),
                cancellationToken);
    }
}
