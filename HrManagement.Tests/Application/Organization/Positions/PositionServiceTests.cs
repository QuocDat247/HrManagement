using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HrManagement.Application.Organization.Positions;
using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Tests.Application.Organization.Positions;
public sealed class PositionServiceTests
{
    [Fact]
    public async Task CreatePositionAsync_WithValidRequest_CreatesPosition()
    {
        var repository =
            new InMemoryPositionRepository();

        var service =
            new PositionService(repository);

        PositionOperationResult result =
            await service.CreatePositionAsync(
                new CreatePositionRequest(
                    " dev ",
                    " Lập trình viên "));

        Assert.True(result.IsSuccessful);
        Assert.Null(result.ErrorMessage);

        Position created =
            Assert.Single(repository.Positions);

        Assert.Equal("DEV", created.Code);
        Assert.Equal("Lập trình viên", created.Name);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task CreatePositionAsync_WhenCodeAlreadyExists_ReturnsFailure()
    {
        var existing =
            new Position(
                Guid.NewGuid(),
                "DEV",
                "Lập trình viên");

        var repository =
            new InMemoryPositionRepository(
                existing);

        var service =
            new PositionService(repository);

        PositionOperationResult result =
            await service.CreatePositionAsync(
                new CreatePositionRequest(
                    " dev ",
                    "Developer khác"));

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            "Mã chức danh đã tồn tại.",
            result.ErrorMessage);

        Assert.Single(repository.Positions);
    }

    [Fact]
    public async Task UpdatePositionAsync_WhenPositionIsInactive_PreservesInactiveStatus()
    {
        var position =
            new Position(
                Guid.NewGuid(),
                "DEV",
                "Lập trình viên",
                false);

        var repository =
            new InMemoryPositionRepository(
                position);

        var service =
            new PositionService(repository);

        PositionOperationResult result =
            await service.UpdatePositionAsync(
                new UpdatePositionRequest(
                    position.Id,
                    "SWE",
                    "Kỹ sư phần mềm"));

        Assert.True(result.IsSuccessful);

        Position updated =
            Assert.Single(repository.Positions);

        Assert.Equal("SWE", updated.Code);
        Assert.Equal("Kỹ sư phần mềm", updated.Name);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task UpdatePositionAsync_WhenCodeBelongsToAnotherPosition_ReturnsFailure()
    {
        var developer =
            new Position(
                Guid.NewGuid(),
                "DEV",
                "Lập trình viên");

        var manager =
            new Position(
                Guid.NewGuid(),
                "MGR",
                "Trưởng phòng");

        var repository =
            new InMemoryPositionRepository(
                developer,
                manager);

        var service =
            new PositionService(repository);

        PositionOperationResult result =
            await service.UpdatePositionAsync(
                new UpdatePositionRequest(
                    developer.Id,
                    " mgr ",
                    "Tên mới"));

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            "Mã chức danh đã tồn tại.",
            result.ErrorMessage);

        Position persisted =
            repository.Positions.Single(
                position =>
                    position.Id == developer.Id);

        Assert.Equal("DEV", persisted.Code);
    }

    [Fact]
    public async Task DeactivatePositionAsync_WhenPositionIsActive_DeactivatesPosition()
    {
        var position =
            new Position(
                Guid.NewGuid(),
                "HR-SPEC",
                "Chuyên viên nhân sự");

        var repository =
            new InMemoryPositionRepository(
                position);

        var service =
            new PositionService(repository);

        PositionOperationResult result =
            await service.DeactivatePositionAsync(
                position.Id);

        Assert.True(result.IsSuccessful);

        Position updated =
            Assert.Single(repository.Positions);

        Assert.False(updated.IsActive);
        Assert.Equal(position.Id, updated.Id);
        Assert.Equal("HR-SPEC", updated.Code);
    }

    [Fact]
    public async Task ReactivatePositionAsync_WhenPositionIsInactive_ReactivatesPosition()
    {
        var position =
            new Position(
                Guid.NewGuid(),
                "MGR",
                "Trưởng phòng",
                false);

        var repository =
            new InMemoryPositionRepository(
                position);

        var service =
            new PositionService(repository);

        PositionOperationResult result =
            await service.ReactivatePositionAsync(
                position.Id);

        Assert.True(result.IsSuccessful);

        Position updated =
            Assert.Single(repository.Positions);

        Assert.True(updated.IsActive);
    }

    [Fact]
    public async Task DeactivatePositionAsync_WhenPositionDoesNotExist_ReturnsFailure()
    {
        var repository =
            new InMemoryPositionRepository();

        var service =
            new PositionService(repository);

        PositionOperationResult result =
            await service.DeactivatePositionAsync(
                Guid.NewGuid());

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            "Không tìm thấy chức danh.",
            result.ErrorMessage);

        Assert.Empty(repository.Positions);
    }

    private sealed class InMemoryPositionRepository
    : IPositionRepository
    {
        private readonly List<Position>
            _positions;

        public IReadOnlyList<Position> Positions =>
            _positions;

        public InMemoryPositionRepository(
            params Position[] positions)
        {
            _positions =
                positions.ToList();
        }

        public Task<IReadOnlyList<Position>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                IReadOnlyList<Position>>(
                    _positions.ToList());
        }

        public Task<Position?> GetByIdAsync(
            Guid positionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _positions.FirstOrDefault(
                    position =>
                        position.Id == positionId));
        }

        public Task<Position?> GetByCodeAsync(
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _positions.FirstOrDefault(
                    position =>
                        string.Equals(
                            position.Code,
                            code.Trim(),
                            StringComparison.OrdinalIgnoreCase)));
        }

        public Task AddAsync(
            Position position,
            CancellationToken cancellationToken = default)
        {
            _positions.Add(position);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            Position position,
            CancellationToken cancellationToken = default)
        {
            int index =
                _positions.FindIndex(
                    existing =>
                        existing.Id == position.Id);

            if (index >= 0)
            {
                _positions[index] =
                    position;
            }

            return Task.CompletedTask;
        }
    }
}
