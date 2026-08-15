using HrManagement.Application.Organization.Memberships;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Organization.Memberships;

public sealed class EfOrganizationMembershipQueryService
    : IOrganizationMembershipQueryService
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfOrganizationMembershipQueryService(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<IReadOnlyList<OrganizationStaffingCount>>
    GetDepartmentStaffingCountsAsync(
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.Employees
            .AsNoTracking()
            .Where(employee =>
                employee.DepartmentId.HasValue)
            .GroupBy(employee =>
                employee.DepartmentId!.Value)
            .Select(group =>
                new OrganizationStaffingCount(
                    group.Key,

                    group.Sum(employee =>
                        employee.Status == EmployeeStatus.Active
                            ? 1
                            : 0),

                    group.Sum(employee =>
                        employee.Status == EmployeeStatus.OnLeave
                            ? 1
                            : 0),

                    group.Sum(employee =>
                        employee.Status == EmployeeStatus.Inactive
                            ? 1
                            : 0)))
            .ToListAsync(
                cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationStaffingCount>>
    GetPositionStaffingCountsAsync(
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.Employees
            .AsNoTracking()
            .Where(employee =>
                employee.PositionId.HasValue)
            .GroupBy(employee =>
                employee.PositionId!.Value)
            .Select(group =>
                new OrganizationStaffingCount(
                    group.Key,

                    group.Sum(employee =>
                        employee.Status == EmployeeStatus.Active
                            ? 1
                            : 0),

                    group.Sum(employee =>
                        employee.Status == EmployeeStatus.OnLeave
                            ? 1
                            : 0),

                    group.Sum(employee =>
                        employee.Status == EmployeeStatus.Inactive
                            ? 1
                            : 0)))
            .ToListAsync(
                cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationEmployeeListItem>>
    GetEmployeesByDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Department ID không hợp lệ.",
                nameof(departmentId));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        IQueryable<Employee> query =
            dbContext.Employees
                .AsNoTracking()
                .Where(employee =>
                    employee.DepartmentId ==
                    departmentId);

        return await ExecuteQueryAsync(
            dbContext,
            query,
            cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationEmployeeListItem>>
        GetEmployeesByPositionAsync(
            Guid positionId,
            CancellationToken cancellationToken = default)
    {
        if (positionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Position ID không hợp lệ.",
                nameof(positionId));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        IQueryable<Employee> query =
            dbContext.Employees
                .AsNoTracking()
                .Where(employee =>
                    employee.PositionId ==
                    positionId);

        return await ExecuteQueryAsync(
            dbContext,
            query,
            cancellationToken);
    }

    private static async Task<IReadOnlyList<OrganizationEmployeeListItem>>
    ExecuteQueryAsync(
        HrManagementDbContext dbContext,
        IQueryable<Employee> query,
        CancellationToken cancellationToken)
    {
        List<EmployeeSnapshot> employees =
            await query
                .Select(employee =>
                    new EmployeeSnapshot(
                        employee.Id,
                        employee.EmployeeCode,
                        employee.FullName,
                        employee.Department,
                        employee.Position,
                        employee.DepartmentId,
                        employee.PositionId,
                        employee.Status,
                        employee.HireDate))
                .ToListAsync(
                    cancellationToken);

        if (employees.Count == 0)
        {
            return Array.Empty<
                OrganizationEmployeeListItem>();
        }

        Dictionary<Guid, string> departmentNames =
            await dbContext.Departments
                .AsNoTracking()
                .ToDictionaryAsync(
                    department => department.Id,
                    department => department.Name,
                    cancellationToken);

        Dictionary<Guid, string> positionNames =
            await dbContext.Positions
                .AsNoTracking()
                .ToDictionaryAsync(
                    position => position.Id,
                    position => position.Name,
                    cancellationToken);

        return employees
            .Select(employee =>
                new OrganizationEmployeeListItem(
                    employee.EmployeeId,
                    employee.EmployeeCode,
                    employee.FullName,

                    ResolveName(
                        employee.DepartmentId,
                        employee.LegacyDepartmentName,
                        departmentNames),

                    ResolveName(
                        employee.PositionId,
                        employee.LegacyPositionName,
                        positionNames),

                    employee.Status,
                    employee.HireDate))
            .OrderBy(employee =>
                employee.FullName)
            .ThenBy(employee =>
                employee.EmployeeCode)
            .ToList();
    }

    private static string ResolveName(
    Guid? masterId,
    string legacyName,
    IReadOnlyDictionary<Guid, string> masterNames)
    {
        if (masterId.HasValue
            && masterNames.TryGetValue(
                masterId.Value,
                out string? masterName))
        {
            return masterName;
        }

        return legacyName;
    }

    private sealed record EmployeeSnapshot(
    Guid EmployeeId,
    string EmployeeCode,
    string FullName,
    string LegacyDepartmentName,
    string LegacyPositionName,
    Guid? DepartmentId,
    Guid? PositionId,
    EmployeeStatus Status,
    DateOnly HireDate);
}
