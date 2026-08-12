using HrManagement.Application.Employees.EmploymentHistories;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Employees;

public sealed class EfEmploymentHistoryBackfillService
    : IEmploymentHistoryBackfillService
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfEmploymentHistoryBackfillService(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<EmploymentHistoryBackfillResult>
        BackfillAsync(
            CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        List<EmployeeSnapshot> employees =
            await dbContext.Employees
                .AsNoTracking()
                .Select(employee =>
                    new EmployeeSnapshot(
                        employee.Id,
                        employee.HireDate,
                        employee.Status,
                        employee.TerminationDate))
                .ToListAsync(cancellationToken);

        List<Guid> employeeIdsWithHistory =
            await dbContext.EmploymentPeriods
                .AsNoTracking()
                .Select(period =>
                    period.EmployeeId)
                .Distinct()
                .ToListAsync(cancellationToken);

        HashSet<Guid> employeesWithHistory =
            employeeIdsWithHistory.ToHashSet();

        int createdPeriods = 0;
        int skippedExistingHistory = 0;
        int skippedIncompleteLegacyRecords = 0;

        foreach (EmployeeSnapshot employee in employees)
        {
            if (employeesWithHistory.Contains(
                    employee.Id))
            {
                skippedExistingHistory++;
                continue;
            }

            if (employee.Status ==
                    EmployeeStatus.Inactive
                && employee.TerminationDate is null)
            {
                skippedIncompleteLegacyRecords++;
                continue;
            }

            EmploymentPeriod period =
                employee.Status ==
                    EmployeeStatus.Inactive
                    ? new EmploymentPeriod(
                        Guid.NewGuid(),
                        employee.Id,
                        employee.HireDate,
                        employee.TerminationDate)
                    : new EmploymentPeriod(
                        Guid.NewGuid(),
                        employee.Id,
                        employee.HireDate);

            dbContext.EmploymentPeriods.Add(
                period);

            createdPeriods++;
        }

        if (createdPeriods > 0)
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }

        return new EmploymentHistoryBackfillResult(
            ScannedEmployees: employees.Count,
            CreatedPeriods: createdPeriods,
            SkippedExistingHistory:
                skippedExistingHistory,
            SkippedIncompleteLegacyRecords:
                skippedIncompleteLegacyRecords);
    }

    private sealed record EmployeeSnapshot(
        Guid Id,
        DateOnly HireDate,
        EmployeeStatus Status,
        DateOnly? TerminationDate);
}
