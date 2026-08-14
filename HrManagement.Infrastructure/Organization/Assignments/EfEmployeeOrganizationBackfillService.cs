using HrManagement.Application.Organization.Assignments;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Organization.Assignments;

// Infrastructure implementation
public sealed class EfEmployeeOrganizationBackfillService
    : IEmployeeOrganizationBackfillService
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfEmployeeOrganizationBackfillService(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<EmployeeOrganizationBackfillResult>
        BackfillAsync(
            CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        List<Employee> employees =
            await dbContext.Employees
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        List<Department> departments =
            await dbContext.Departments
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        List<Position> positions =
            await dbContext.Positions
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        int updatedEmployees = 0;

        int assignedDepartmentReferences = 0;
        int assignedPositionReferences = 0;

        int unresolvedDepartmentReferences = 0;
        int unresolvedPositionReferences = 0;

        int ambiguousDepartmentReferences = 0;
        int ambiguousPositionReferences = 0;

        foreach (Employee employee in employees)
        {
            Guid? departmentId =
                employee.DepartmentId;

            Guid? positionId =
                employee.PositionId;

            bool changed = false;

            if (!departmentId.HasValue)
            {
                ReferenceResolution departmentResolution =
                    ResolveDepartment(
                        employee.Department,
                        departments);

                if (departmentResolution.IsResolved)
                {
                    departmentId =
                        departmentResolution.Id;

                    assignedDepartmentReferences++;
                    changed = true;
                }
                else if (departmentResolution.IsAmbiguous)
                {
                    ambiguousDepartmentReferences++;
                }
                else
                {
                    unresolvedDepartmentReferences++;
                }
            }

            if (!positionId.HasValue)
            {
                ReferenceResolution positionResolution =
                    ResolvePosition(
                        employee.Position,
                        positions);

                if (positionResolution.IsResolved)
                {
                    positionId =
                        positionResolution.Id;

                    assignedPositionReferences++;
                    changed = true;
                }
                else if (positionResolution.IsAmbiguous)
                {
                    ambiguousPositionReferences++;
                }
                else
                {
                    unresolvedPositionReferences++;
                }
            }

            if (!changed)
            {
                continue;
            }

            var updatedEmployee =
                new Employee(
                    employee.Id,
                    employee.EmployeeCode,
                    employee.FullName,
                    employee.Email,
                    employee.PhoneNumber,
                    employee.DateOfBirth,
                    employee.HireDate,
                    employee.Department,
                    employee.Position,
                    employee.Status,
                    employee.TerminationDate,
                    departmentId,
                    positionId);

            dbContext.Employees.Update(
                updatedEmployee);

            updatedEmployees++;
        }

        if (updatedEmployees > 0)
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }

        return new EmployeeOrganizationBackfillResult(
            ScannedEmployees:
                employees.Count,

            UpdatedEmployees:
                updatedEmployees,

            AssignedDepartmentReferences:
                assignedDepartmentReferences,

            AssignedPositionReferences:
                assignedPositionReferences,

            UnresolvedDepartmentReferences:
                unresolvedDepartmentReferences,

            UnresolvedPositionReferences:
                unresolvedPositionReferences,

            AmbiguousDepartmentReferences:
                ambiguousDepartmentReferences,

            AmbiguousPositionReferences:
                ambiguousPositionReferences);
    }

    private static ReferenceResolution ResolveDepartment(
        string legacyValue,
        IReadOnlyList<Department> departments)
    {
        Guid[] candidateIds =
            departments
                .Where(department =>
                    Matches(
                        legacyValue,
                        department.Code,
                        department.Name))
                .Select(department =>
                    department.Id)
                .Distinct()
                .ToArray();

        return ResolveCandidateIds(
            candidateIds);
    }

    private static ReferenceResolution ResolvePosition(
        string legacyValue,
        IReadOnlyList<Position> positions)
    {
        Guid[] candidateIds =
            positions
                .Where(position =>
                    Matches(
                        legacyValue,
                        position.Code,
                        position.Name))
                .Select(position =>
                    position.Id)
                .Distinct()
                .ToArray();

        return ResolveCandidateIds(
            candidateIds);
    }

    private static bool Matches(
        string legacyValue,
        string code,
        string name)
    {
        string normalizedValue =
            legacyValue.Trim();

        return string.Equals(
                   normalizedValue,
                   code,
                   StringComparison.OrdinalIgnoreCase)
               || string.Equals(
                   normalizedValue,
                   name,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static ReferenceResolution ResolveCandidateIds(
        IReadOnlyList<Guid> candidateIds)
    {
        return candidateIds.Count switch
        {
            1 => new ReferenceResolution(
                candidateIds[0],
                false),

            > 1 => new ReferenceResolution(
                null,
                true),

            _ => new ReferenceResolution(
                null,
                false)
        };
    }

    private sealed record ReferenceResolution(
        Guid? Id,
        bool IsAmbiguous)
    {
        public bool IsResolved =>
            Id.HasValue;
    }
}
