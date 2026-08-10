using HrManagement.Application.Dashboard;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Dashboard;

public sealed class EfDashboardService : IDashboardService
{
    private readonly IDbContextFactory<HrManagementDbContext> _dbContextFactory;

    public EfDashboardService(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<DashboardSummary> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var statusCounts =
            await dbContext.Employees
                .AsNoTracking()
                .GroupBy(employee => employee.Status)
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .ToListAsync(cancellationToken);

        int activeEmployees =
            statusCounts
                .Where(item =>
                    item.Status == EmployeeStatus.Active)
                .Sum(item => item.Count);

        int employeesOnLeave =
            statusCounts
                .Where(item =>
                    item.Status == EmployeeStatus.OnLeave)
                .Sum(item => item.Count);

        int inactiveEmployees =
            statusCounts
                .Where(item =>
                    item.Status == EmployeeStatus.Inactive)
                .Sum(item => item.Count);

        int totalEmployees =
            statusCounts.Sum(item => item.Count);

        return new DashboardSummary(
            TotalEmployees: totalEmployees,
            ActiveEmployees: activeEmployees,
            EmployeesOnLeave: employeesOnLeave,
            InactiveEmployees: inactiveEmployees);
    }
}
