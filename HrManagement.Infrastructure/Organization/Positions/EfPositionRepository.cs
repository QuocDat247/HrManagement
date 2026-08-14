using HrManagement.Application.Organization.Positions;
using HrManagement.Domain.Organization.Positions;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Organization.Positions;

public sealed class EfPositionRepository
    : IPositionRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfPositionRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<IReadOnlyList<Position>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.Positions
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Position?> GetByIdAsync(
        Guid positionId,
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.Positions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                position =>
                    position.Id == positionId,
                cancellationToken);
    }

    public async Task<Position?> GetByCodeAsync(
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

        return await dbContext.Positions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                position =>
                    position.Code == normalizedCode,
                cancellationToken);
    }

    public async Task AddAsync(
        Position position,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(position);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        dbContext.Positions.Add(
            position);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task UpdateAsync(
        Position position,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(position);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        dbContext.Positions.Update(
            position);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
