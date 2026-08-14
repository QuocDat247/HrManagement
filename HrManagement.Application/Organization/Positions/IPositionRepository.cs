using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Application.Organization.Positions;

public interface IPositionRepository
{
    Task<IReadOnlyList<Position>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Position?> GetByIdAsync(
        Guid positionId,
        CancellationToken cancellationToken = default);

    Task<Position?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Position position,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Position position,
        CancellationToken cancellationToken = default);
}
