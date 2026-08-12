using HrManagement.Application.Employees.EmploymentHistories;
using HrManagement.Domain.Employees;

namespace HrManagement.Tests.Employees;
public sealed class EmploymentHistoryServiceTests
{
    [Fact]
    public async Task GetHistoryAsync_WithMultiplePeriods_ReturnsOrderedReadModel()
    {
        Guid employeeId =
            Guid.NewGuid();

        var firstPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2022, 1, 1),
                new DateOnly(2024, 6, 30));

        var secondPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2025, 2, 1),
                new DateOnly(2026, 3, 15));

        var thirdPeriod =
            new EmploymentPeriod(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(2026, 8, 1));

        var repository =
            new StubEmploymentHistoryRepository
            {
                History =
                    new EmploymentHistory(
                        employeeId,
                        [
                            thirdPeriod,
                        firstPeriod,
                        secondPeriod
                        ])
            };

        var service =
            new EmploymentHistoryService(
                repository);

        EmployeeEmploymentHistoryDetails result =
            await service.GetHistoryAsync(
                employeeId);

        Assert.Equal(
            employeeId,
            result.EmployeeId);

        Assert.Equal(
            3,
            result.Periods.Count);

        Assert.Equal(
            1,
            result.Periods[0].SequenceNumber);

        Assert.Equal(
            firstPeriod.Id,
            result.Periods[0].Id);

        Assert.Equal(
            2,
            result.Periods[1].SequenceNumber);

        Assert.Equal(
            secondPeriod.Id,
            result.Periods[1].Id);

        Assert.Equal(
            3,
            result.Periods[2].SequenceNumber);

        Assert.Equal(
            thirdPeriod.Id,
            result.Periods[2].Id);

        Assert.True(
            result.Periods[2].IsOpen);

        Assert.Null(
            result.Periods[2].EndDate);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenHistoryIsEmpty_ReturnsEmptyPeriods()
    {
        Guid employeeId =
            Guid.NewGuid();

        var repository =
            new StubEmploymentHistoryRepository
            {
                History =
                    new EmploymentHistory(
                        employeeId,
                        [])
            };

        var service =
            new EmploymentHistoryService(
                repository);

        EmployeeEmploymentHistoryDetails result =
            await service.GetHistoryAsync(
                employeeId);

        Assert.Equal(
            employeeId,
            result.EmployeeId);

        Assert.Empty(
            result.Periods);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenEmployeeIdIsEmpty_Throws()
    {
        var repository =
            new StubEmploymentHistoryRepository();

        var service =
            new EmploymentHistoryService(
                repository);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetHistoryAsync(
                Guid.Empty));
    }

    private sealed class StubEmploymentHistoryRepository
    : IEmploymentHistoryRepository
    {
        public EmploymentHistory? History
        {
            get;
            set;
        }

        public Task<EmploymentHistory>
            GetByEmployeeIdAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                History
                ?? new EmploymentHistory(
                    employeeId,
                    []));
        }

        public Task AddPeriodAsync(
            EmploymentPeriod period,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UpdatePeriodAsync(
            EmploymentPeriod period,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
