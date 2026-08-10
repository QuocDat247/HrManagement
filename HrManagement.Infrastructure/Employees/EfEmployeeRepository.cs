using HrManagement.Application.Employees;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Employees;

public sealed class EfEmployeeRepository : IEmployeeRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfEmployeeRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<Employee>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.Employees
            .AsNoTracking()
            .OrderBy(employee => employee.EmployeeCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<Employee?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                employee => employee.Id == id,
                cancellationToken);
    }

    public async Task<Employee?> GetByEmployeeCodeAsync(
        string employeeCode,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        string normalizedCode = employeeCode.Trim();

        return await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                employee =>
                    employee.EmployeeCode == normalizedCode,
                cancellationToken);
    }

    public async Task AddAsync(
        Employee employee,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        await dbContext.Employees.AddAsync(
            employee,
            cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task UpdateAsync(
        Employee employee,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        dbContext.Employees.Update(employee);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
