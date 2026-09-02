using HrManagement.Application.Payroll.Compensation;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Payroll.Compensation;

public sealed class EfEmployeeCompensationQuerySource
    : IEmployeeCompensationQuerySource
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfEmployeeCompensationQuerySource(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<IReadOnlyList<EmployeeCompensationSegment>>
        GetForPeriodAsync(
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

        return await dbContext
            .EmployeeCompensations
            .AsNoTracking()
            .Where(
                compensation =>
                    normalizedEmployeeIds.Contains(
                        compensation.EmployeeId)
                    && compensation.EffectiveFrom <=
                        periodEnd
                    && (
                        !compensation.EffectiveTo.HasValue
                        || compensation.EffectiveTo.Value >=
                            periodStart
                    ))
            .OrderBy(
                compensation =>
                    compensation.EmployeeId)
            .ThenBy(
                compensation =>
                    compensation.EffectiveFrom)
            .ThenBy(
                compensation =>
                    compensation.Id)
            .Select(
                compensation =>
                    new EmployeeCompensationSegment(
                        compensation.Id,
                        compensation.EmployeeId,
                        compensation.EmploymentPeriodId,
                        compensation.EffectiveFrom,
                        compensation.EffectiveTo,
                        compensation.MonthlyBaseSalary,
                        compensation.CurrencyCode))
            .ToArrayAsync(
                cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeCompensationSegment>>
        GetHistoryAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .EmployeeCompensations
            .AsNoTracking()
            .Where(
                compensation =>
                    compensation.EmployeeId ==
                        employeeId)
            .OrderByDescending(
                compensation =>
                    compensation.EffectiveFrom)
            .ThenByDescending(
                compensation =>
                    compensation.Id)
            .Select(
                compensation =>
                    new EmployeeCompensationSegment(
                        compensation.Id,
                        compensation.EmployeeId,
                        compensation.EmploymentPeriodId,
                        compensation.EffectiveFrom,
                        compensation.EffectiveTo,
                        compensation.MonthlyBaseSalary,
                        compensation.CurrencyCode))
            .ToArrayAsync(
                cancellationToken);
    }
}
