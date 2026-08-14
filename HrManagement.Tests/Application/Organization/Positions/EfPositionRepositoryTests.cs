using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HrManagement.Domain.Organization.Positions;
using HrManagement.Infrastructure.Organization.Positions;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using static HrManagement.Tests.Employees.EfEmploymentHistoryRepositoryTests;

namespace HrManagement.Tests.Application.Organization.Positions;
public sealed class EfPositionRepositoryTests
{
    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsPosition()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();
        }

        var repository =
            new EfPositionRepository(
                new TestDbContextFactory(options));

        var position =
            new Position(
                Guid.NewGuid(),
                "DEV",
                "Lập trình viên");

        await repository.AddAsync(
            position);

        Position? persisted =
            await repository.GetByIdAsync(
                position.Id);

        Assert.NotNull(persisted);

        Assert.Equal(
            position.Id,
            persisted.Id);

        Assert.Equal(
            "DEV",
            persisted.Code);

        Assert.Equal(
            "Lập trình viên",
            persisted.Name);

        Assert.True(
            persisted.IsActive);
    }

    [Fact]
    public async Task GetByCodeAsync_WithUnnormalizedCode_ReturnsPosition()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Positions.Add(
                new Position(
                    Guid.NewGuid(),
                    "MGR",
                    "Trưởng phòng"));

            await dbContext.SaveChangesAsync();
        }

        var repository =
            new EfPositionRepository(
                new TestDbContextFactory(options));

        Position? position =
            await repository.GetByCodeAsync(
                " mgr ");

        Assert.NotNull(position);

        Assert.Equal(
            "MGR",
            position.Code);
    }

    [Fact]
    public async Task UpdateAsync_PersistsPositionChanges()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        Guid positionId =
            Guid.NewGuid();

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Positions.Add(
                new Position(
                    positionId,
                    "DEV",
                    "Lập trình viên"));

            await dbContext.SaveChangesAsync();
        }

        var repository =
            new EfPositionRepository(
                new TestDbContextFactory(options));

        var updatedPosition =
            new Position(
                positionId,
                "SWE",
                "Kỹ sư phần mềm",
                false);

        await repository.UpdateAsync(
            updatedPosition);

        Position? persisted =
            await repository.GetByIdAsync(
                positionId);

        Assert.NotNull(persisted);

        Assert.Equal(
            "SWE",
            persisted.Code);

        Assert.Equal(
            "Kỹ sư phần mềm",
            persisted.Name);

        Assert.False(
            persisted.IsActive);
    }

    [Fact]
    public async Task AddAsync_WhenCodeAlreadyExists_ThrowsDbUpdateException()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        DbContextOptions<HrManagementDbContext> options =
            new DbContextOptionsBuilder<HrManagementDbContext>()
                .UseSqlite(connection)
                .Options;

        await using (var dbContext =
                     new HrManagementDbContext(options))
        {
            await dbContext.Database
                .EnsureCreatedAsync();
        }

        var repository =
            new EfPositionRepository(
                new TestDbContextFactory(options));

        await repository.AddAsync(
            new Position(
                Guid.NewGuid(),
                "DEV",
                "Lập trình viên"));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => repository.AddAsync(
                new Position(
                    Guid.NewGuid(),
                    "DEV",
                    "Developer khác")));
    }
}
