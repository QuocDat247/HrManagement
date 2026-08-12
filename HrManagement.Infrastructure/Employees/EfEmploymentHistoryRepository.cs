using HrManagement.Application.Employees.EmploymentHistories;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Employees;

public sealed class EfEmploymentHistoryRepository
    : IEmploymentHistoryRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfEmploymentHistoryRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<EmploymentHistory>
        GetByEmployeeIdAsync(
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
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        List<EmploymentPeriod> periods =
            await dbContext.EmploymentPeriods
                .AsNoTracking()
                .Where(period =>
                    period.EmployeeId == employeeId)
                .OrderBy(period =>
                    period.StartDate)
                .ThenBy(period =>
                    period.Id)
                .ToListAsync(cancellationToken);

        return new EmploymentHistory(
            employeeId,
            periods);
    }

    public async Task AddPeriodAsync(
        EmploymentPeriod period,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(period);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        dbContext.EmploymentPeriods.Add(period);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task UpdatePeriodAsync(
    EmploymentPeriod period,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(period);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        dbContext.EmploymentPeriods.Update(period);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
