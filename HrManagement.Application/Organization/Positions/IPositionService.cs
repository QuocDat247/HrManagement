using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Application.Organization.Positions;

public interface IPositionService
{
    Task<IReadOnlyList<Position>> GetPositionsAsync(
        CancellationToken cancellationToken = default);

    Task<PositionOperationResult> CreatePositionAsync(
        CreatePositionRequest request,
        CancellationToken cancellationToken = default);

    Task<PositionOperationResult> UpdatePositionAsync(
        UpdatePositionRequest request,
        CancellationToken cancellationToken = default);

    Task<PositionOperationResult> DeactivatePositionAsync(
        Guid positionId,
        CancellationToken cancellationToken = default);

    Task<PositionOperationResult> ReactivatePositionAsync(
        Guid positionId,
        CancellationToken cancellationToken = default);
}
