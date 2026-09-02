using HrManagement.Application.Payroll.Compensation;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Payroll.Compensation;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Payroll.Compensation;

public sealed class EfEmployeeCompensationContextSource
    : IEmployeeCompensationContextSource
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfEmployeeCompensationContextSource(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<EmployeeCompensationContext?> GetAsync(
        Guid employeeId,
        DateOnly effectiveFrom,
        CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        if (effectiveFrom == default)
        {
            throw new ArgumentException(
                "Ngày hiệu lực lương không hợp lệ.",
                nameof(effectiveFrom));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        EmploymentPeriod[] matchingPeriods =
            await dbContext
                .EmploymentPeriods
                .AsNoTracking()
                .Where(
                    period =>
                        period.EmployeeId ==
                            employeeId
                        && period.StartDate <=
                            effectiveFrom
                        && (
                            !period.EndDate.HasValue
                            || period.EndDate.Value >=
                                effectiveFrom
                        ))
                .OrderBy(
                    period =>
                        period.StartDate)
                .ThenBy(
                    period =>
                        period.Id)
                .Take(
                    2)
                .ToArrayAsync(
                    cancellationToken);

        if (matchingPeriods.Length > 1)
        {
            throw new InvalidOperationException(
                "Có nhiều giai đoạn làm việc cùng bao phủ ngày hiệu lực lương.");
        }

        EmploymentPeriod? employmentPeriod =
            matchingPeriods.SingleOrDefault();

        if (employmentPeriod is null)
        {
            return null;
        }

        EmployeeCompensation[] openCompensations =
            await dbContext
                .EmployeeCompensations
                .AsNoTracking()
                .Where(
                    compensation =>
                        compensation.EmploymentPeriodId ==
                            employmentPeriod.Id
                        && !compensation.EffectiveTo.HasValue)
                .OrderBy(
                    compensation =>
                        compensation.EffectiveFrom)
                .ThenBy(
                    compensation =>
                        compensation.Id)
                .Take(
                    2)
                .ToArrayAsync(
                    cancellationToken);

        if (openCompensations.Length > 1)
        {
            throw new InvalidOperationException(
                "Có nhiều cấu hình lương đang mở trong cùng giai đoạn làm việc.");
        }

        return new EmployeeCompensationContext(
            employmentPeriod,
            openCompensations.SingleOrDefault());
    }
}
