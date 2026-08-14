using HrManagement.Application.Organization.Departments;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Organization.Departments;

public sealed class EfDepartmentRepository
    : IDepartmentRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfDepartmentRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<IReadOnlyList<Department>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.Departments
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Department?> GetByIdAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.Departments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                department =>
                    department.Id == departmentId,
                cancellationToken);
    }

    public async Task<Department?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        string normalizedCode =
            code.Trim()
                .ToUpperInvariant();

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.Departments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                department =>
                    department.Code == normalizedCode,
                cancellationToken);
    }

    public async Task AddAsync(
        Department department,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(department);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        dbContext.Departments.Add(
            department);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task UpdateAsync(
        Department department,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(department);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        dbContext.Departments.Update(
            department);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
