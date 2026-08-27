using HrManagement.Application.Overtime.Requests;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Overtime.Requests;

public sealed class EfOvertimeRequestSubmissionContextSource
    : IOvertimeRequestSubmissionContextSource
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfOvertimeRequestSubmissionContextSource(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<EmploymentPeriod?> GetEmploymentPeriodAsync(
        Guid employeeId,
        DateOnly workDate,
        CancellationToken cancellationToken = default)
    {
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
                            workDate
                        && (
                            !period.EndDate.HasValue
                            || period.EndDate.Value >=
                                workDate
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
                "Có nhiều giai đoạn làm việc cùng bao phủ ngày tăng ca.");
        }

        return matchingPeriods
            .SingleOrDefault();
    }
}
