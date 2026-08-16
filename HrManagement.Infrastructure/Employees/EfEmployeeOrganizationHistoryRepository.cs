using HrManagement.Application.Employees.OrganizationAssignments;
using HrManagement.Domain.Employees.OrganizationAssignments;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Employees;

public sealed class EfEmployeeOrganizationHistoryRepository
    : IEmployeeOrganizationHistoryRepository
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfEmployeeOrganizationHistoryRepository(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<EmployeeOrganizationHistory>
        GetByEmployeeIdAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        List<EmployeeOrganizationAssignment> assignments =
            await dbContext.EmployeeOrganizationAssignments
                .AsNoTracking()
                .Where(
                    assignment =>
                        assignment.EmployeeId ==
                        employeeId)
                .OrderBy(
                    assignment =>
                        assignment.StartDate)
                .ThenBy(
                    assignment =>
                        assignment.Id)
                .ToListAsync(
                    cancellationToken);

        return new EmployeeOrganizationHistory(
            employeeId,
            assignments);
    }

    public async Task AddAssignmentAsync(
        EmployeeOrganizationAssignment assignment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            assignment);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        dbContext.EmployeeOrganizationAssignments.Add(
            assignment);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task UpdateAssignmentAsync(
        EmployeeOrganizationAssignment assignment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            assignment);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        dbContext.EmployeeOrganizationAssignments.Update(
            assignment);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
