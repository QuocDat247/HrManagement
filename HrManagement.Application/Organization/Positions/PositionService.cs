using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Application.Organization.Positions;

public sealed class PositionService
    : IPositionService
{
    private readonly IPositionRepository
        _positionRepository;

    public PositionService(
        IPositionRepository positionRepository)
    {
        _positionRepository =
            positionRepository;
    }

    public async Task<IReadOnlyList<Position>>
        GetPositionsAsync(
            CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Position> positions =
            await _positionRepository
                .GetAllAsync(cancellationToken);

        return positions
            .OrderBy(position => position.Name)
            .ThenBy(position => position.Code)
            .ToList();
    }

    public async Task<PositionOperationResult>
        CreatePositionAsync(
            CreatePositionRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Position position;

        try
        {
            position =
                new Position(
                    Guid.NewGuid(),
                    request.Code,
                    request.Name);
        }
        catch (ArgumentException exception)
        {
            return new PositionOperationResult(
                false,
                exception.Message);
        }

        Position? existingPosition =
            await _positionRepository
                .GetByCodeAsync(
                    position.Code,
                    cancellationToken);

        if (existingPosition is not null)
        {
            return new PositionOperationResult(
                false,
                "Mã chức danh đã tồn tại.");
        }

        await _positionRepository.AddAsync(
            position,
            cancellationToken);

        return new PositionOperationResult(
            true,
            null);
    }

    public async Task<PositionOperationResult>
        UpdatePositionAsync(
            UpdatePositionRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Position? existingPosition =
            await _positionRepository
                .GetByIdAsync(
                    request.PositionId,
                    cancellationToken);

        if (existingPosition is null)
        {
            return new PositionOperationResult(
                false,
                "Không tìm thấy chức danh.");
        }

        Position updatedPosition;

        try
        {
            updatedPosition =
                new Position(
                    existingPosition.Id,
                    request.Code,
                    request.Name,
                    existingPosition.IsActive);
        }
        catch (ArgumentException exception)
        {
            return new PositionOperationResult(
                false,
                exception.Message);
        }

        Position? positionWithSameCode =
            await _positionRepository
                .GetByCodeAsync(
                    updatedPosition.Code,
                    cancellationToken);

        if (positionWithSameCode is not null
            && positionWithSameCode.Id
                != existingPosition.Id)
        {
            return new PositionOperationResult(
                false,
                "Mã chức danh đã tồn tại.");
        }

        await _positionRepository.UpdateAsync(
            updatedPosition,
            cancellationToken);

        return new PositionOperationResult(
            true,
            null);
    }

    public async Task<PositionOperationResult>
        DeactivatePositionAsync(
            Guid positionId,
            CancellationToken cancellationToken = default)
    {
        Position? existingPosition =
            await _positionRepository
                .GetByIdAsync(
                    positionId,
                    cancellationToken);

        if (existingPosition is null)
        {
            return new PositionOperationResult(
                false,
                "Không tìm thấy chức danh.");
        }

        if (!existingPosition.IsActive)
        {
            return new PositionOperationResult(
                true,
                null);
        }

        var deactivatedPosition =
            new Position(
                existingPosition.Id,
                existingPosition.Code,
                existingPosition.Name,
                false);

        await _positionRepository.UpdateAsync(
            deactivatedPosition,
            cancellationToken);

        return new PositionOperationResult(
            true,
            null);
    }

    public async Task<PositionOperationResult>
        ReactivatePositionAsync(
            Guid positionId,
            CancellationToken cancellationToken = default)
    {
        Position? existingPosition =
            await _positionRepository
                .GetByIdAsync(
                    positionId,
                    cancellationToken);

        if (existingPosition is null)
        {
            return new PositionOperationResult(
                false,
                "Không tìm thấy chức danh.");
        }

        if (existingPosition.IsActive)
        {
            return new PositionOperationResult(
                true,
                null);
        }

        var reactivatedPosition =
            new Position(
                existingPosition.Id,
                existingPosition.Code,
                existingPosition.Name,
                true);

        await _positionRepository.UpdateAsync(
            reactivatedPosition,
            cancellationToken);

        return new PositionOperationResult(
            true,
            null);
    }
}
