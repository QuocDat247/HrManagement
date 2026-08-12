using HrManagement.Application.Employees.EmploymentLifecycle;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Employees;

public sealed class EfEmploymentLifecyclePersistence
    : IEmploymentLifecyclePersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfEmploymentLifecyclePersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task UpdateEmployeeWithNewPeriodAsync(
        Employee employee,
        EmploymentPeriod newPeriod,
        CancellationToken cancellationToken = default)
    {
        ValidatePair(
            employee,
            newPeriod);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        dbContext.Employees.Update(
            employee);

        dbContext.EmploymentPeriods.Add(
            newPeriod);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }

    public async Task CreateEmployeeWithPeriodAsync(
        Employee employee,
        EmploymentPeriod period,
        CancellationToken cancellationToken = default)
    {
        ValidatePair(employee, period);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        dbContext.Employees.Add(employee);
        dbContext.EmploymentPeriods.Add(period);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }

    public async Task UpdateEmployeeWithPeriodAsync(
        Employee employee,
        EmploymentPeriod period,
        CancellationToken cancellationToken = default)
    {
        ValidatePair(employee, period);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        dbContext.Employees.Update(employee);
        dbContext.EmploymentPeriods.Update(period);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }

    private static void ValidatePair(
        Employee employee,
        EmploymentPeriod period)
    {
        ArgumentNullException.ThrowIfNull(employee);
        ArgumentNullException.ThrowIfNull(period);

        if (period.EmployeeId != employee.Id)
        {
            throw new ArgumentException(
                "Giai đoạn làm việc không thuộc nhân viên được cung cấp.",
                nameof(period));
        }
    }
}
