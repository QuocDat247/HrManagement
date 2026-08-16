using HrManagement.Application.Employees.OrganizationAssignments;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.OrganizationAssignments;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Employees;

public sealed class EfEmployeeOrganizationAssignmentBackfillService
    : IEmployeeOrganizationAssignmentBackfillService
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfEmployeeOrganizationAssignmentBackfillService(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<EmployeeOrganizationAssignmentBackfillResult>
        BackfillAsync(
            CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        DateOnly today =
            DateOnly.FromDateTime(
                DateTime.Today);

        List<EmployeeSnapshot> employees =
            await dbContext.Employees
                .AsNoTracking()
                .Select(
                    employee =>
                        new EmployeeSnapshot(
                            employee.Id,
                            employee.Status,
                            employee.DepartmentId,
                            employee.PositionId))
                .ToListAsync(
                    cancellationToken);

        Dictionary<Guid, Department> departments =
            await dbContext.Departments
                .AsNoTracking()
                .ToDictionaryAsync(
                    department =>
                        department.Id,
                    cancellationToken);

        Dictionary<Guid, Position> positions =
            await dbContext.Positions
                .AsNoTracking()
                .ToDictionaryAsync(
                    position =>
                        position.Id,
                    cancellationToken);

        List<EmploymentPeriod> periods =
            await dbContext.EmploymentPeriods
                .AsNoTracking()
                .OrderBy(
                    period =>
                        period.StartDate)
                .ThenBy(
                    period =>
                        period.Id)
                .ToListAsync(
                    cancellationToken);

        Dictionary<Guid, List<EmploymentPeriod>>
            periodsByEmployee =
                periods
                    .GroupBy(
                        period =>
                            period.EmployeeId)
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                            group.ToList());

        HashSet<Guid> employeesWithAssignments =
            (
                await dbContext
                    .EmployeeOrganizationAssignments
                    .AsNoTracking()
                    .Select(
                        assignment =>
                            assignment.EmployeeId)
                    .Distinct()
                    .ToListAsync(
                        cancellationToken)
            )
            .ToHashSet();

        int createdAssignments = 0;
        int skippedExistingHistory = 0;
        int skippedMissingOrganizationReferences = 0;
        int skippedMissingMasterData = 0;
        int skippedMissingEmploymentPeriod = 0;
        int skippedInconsistentEmploymentState = 0;

        foreach (EmployeeSnapshot employee in employees)
        {
            if (employeesWithAssignments.Contains(
                    employee.Id))
            {
                skippedExistingHistory++;
                continue;
            }

            if (!employee.DepartmentId.HasValue
                || !employee.PositionId.HasValue)
            {
                skippedMissingOrganizationReferences++;
                continue;
            }

            if (!departments.TryGetValue(
                    employee.DepartmentId.Value,
                    out Department? department)
                || !positions.TryGetValue(
                    employee.PositionId.Value,
                    out Position? position))
            {
                skippedMissingMasterData++;
                continue;
            }

            if (!periodsByEmployee.TryGetValue(
                    employee.Id,
                    out List<EmploymentPeriod>? employeePeriods)
                || employeePeriods.Count == 0)
            {
                skippedMissingEmploymentPeriod++;
                continue;
            }

            EmploymentPeriod? targetPeriod;
            DateOnly assignmentStartDate;
            DateOnly? assignmentEndDate;

            if (employee.Status is
                EmployeeStatus.Active
                or EmployeeStatus.OnLeave)
            {
                targetPeriod =
                    employeePeriods
                        .LastOrDefault(
                            period =>
                                period.IsOpen);

                if (targetPeriod is null
                    || targetPeriod.StartDate > today)
                {
                    skippedInconsistentEmploymentState++;
                    continue;
                }

                assignmentStartDate =
                    targetPeriod.StartDate;

                assignmentEndDate =
                    null;
            }
            else if (employee.Status ==
            EmployeeStatus.Inactive)
            {
                targetPeriod =
                    employeePeriods
                        .LastOrDefault();

                if (targetPeriod is null
                    || targetPeriod.IsOpen
                    || !targetPeriod.EndDate.HasValue)
                {
                    skippedInconsistentEmploymentState++;
                    continue;
                }

                assignmentStartDate =
                    targetPeriod.StartDate;

                assignmentEndDate =
                    targetPeriod.EndDate.Value;
            }
            else
            {
                skippedInconsistentEmploymentState++;
                continue;
            }

            var assignment =
                new EmployeeOrganizationAssignment(
                    Guid.NewGuid(),
                    employee.Id,
                    targetPeriod.Id,
                    department.Id,
                    department.Code,
                    department.Name,
                    position.Id,
                    position.Code,
                    position.Name,
                    assignmentStartDate,
                    assignmentEndDate,
                    isBaseline: true);

            dbContext.EmployeeOrganizationAssignments.Add(
                assignment);

            employeesWithAssignments.Add(
                employee.Id);

            createdAssignments++;
        }

        if (createdAssignments > 0)
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }

        return new EmployeeOrganizationAssignmentBackfillResult(
            ScannedEmployees:
                employees.Count,
            CreatedAssignments:
                createdAssignments,
            SkippedExistingHistory:
                skippedExistingHistory,
            SkippedMissingOrganizationReferences:
                skippedMissingOrganizationReferences,
            SkippedMissingMasterData:
                skippedMissingMasterData,
            SkippedMissingEmploymentPeriod:
                skippedMissingEmploymentPeriod,
            SkippedInconsistentEmploymentState:
                skippedInconsistentEmploymentState);
    }

    private sealed record EmployeeSnapshot(
        Guid Id,
        EmployeeStatus Status,
        Guid? DepartmentId,
        Guid? PositionId);
}
