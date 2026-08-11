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

        List<RecentEmployee> recentEmployees =
            await dbContext.Employees
                .AsNoTracking()
                .OrderByDescending(employee => employee.HireDate)
                .ThenBy(employee => employee.EmployeeCode)
                .Take(5)
                .Select(employee => new RecentEmployee(
                    employee.Id,
                    employee.EmployeeCode,
                    employee.FullName,
                    employee.Department,
                    employee.Position,
                    employee.HireDate,
                    employee.Status))
                .ToListAsync(cancellationToken);

        var departmentCounts =
            await dbContext.Employees
                .AsNoTracking()
                .GroupBy(employee => employee.Department)
                .Select(group => new
            {
                Department = group.Key,
                TotalEmployees = group.Count(),

                ActiveEmployees =
                    group.Count(
                        employee =>
                            employee.Status ==
                            EmployeeStatus.Active),

                EmployeesOnLeave =
                    group.Count(
                        employee =>
                            employee.Status ==
                            EmployeeStatus.OnLeave),

                InactiveEmployees =
                    group.Count(
                        employee =>
                            employee.Status ==
                            EmployeeStatus.Inactive)
                })
                .OrderBy(item => item.Department)
                .ToListAsync(cancellationToken);

        int employeesMissingProfileInformation =
            await dbContext.Employees
                .AsNoTracking()
                .CountAsync(
                employee =>
                    employee.Status != EmployeeStatus.Inactive
                    &&
                    (
                    employee.Email == null
                    || employee.PhoneNumber == null
                    || employee.DateOfBirth == null
                    ),
                    cancellationToken);

        List<DepartmentEmployeeSummary> departments =
            departmentCounts
                .Select(item =>
                    new DepartmentEmployeeSummary(
                        item.Department,
                        item.TotalEmployees,
                        item.ActiveEmployees,
                        item.EmployeesOnLeave,
                        item.InactiveEmployees))
                .ToList();

        return new DashboardSummary(
            TotalEmployees: totalEmployees,
            ActiveEmployees: activeEmployees,
            EmployeesOnLeave: employeesOnLeave,
            InactiveEmployees: inactiveEmployees,
            EmployeesMissingProfileInformation:
                employeesMissingProfileInformation,
            RecentEmployees: recentEmployees,
            Departments: departments);
    }
}
