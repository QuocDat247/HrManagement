using HrManagement.Application.Employees.EmploymentLifecycle;
using HrManagement.Domain.Employees;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using HrManagement.Domain.Employees.OrganizationAssignments;

namespace HrManagement.Infrastructure.Employees;

public sealed class EfEmploymentLifecyclePersistence
    : IEmploymentLifecyclePersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfEmploymentLifecyclePersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task CreateEmployeeWithPeriodAndAssignmentAsync(
        Employee employee,
        EmploymentPeriod period,
        EmployeeOrganizationAssignment assignment,
        CancellationToken cancellationToken = default)
    {
        ValidateTriple(
            employee,
            period,
            assignment);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        dbContext.Employees.Add(
            employee);

        dbContext.EmploymentPeriods.Add(
            period);

        dbContext.EmployeeOrganizationAssignments.Add(
            assignment);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }

    public async Task UpdateEmployeeWithPeriodAndAssignmentAsync(
        Employee employee,
        EmploymentPeriod period,
        EmployeeOrganizationAssignment assignment,
        CancellationToken cancellationToken = default)
    {
        ValidateTriple(
            employee,
            period,
            assignment);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        dbContext.Employees.Update(
            employee);

        dbContext.EmploymentPeriods.Update(
            period);

        dbContext.EmployeeOrganizationAssignments.Update(
            assignment);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }

    public async Task UpdateEmployeeWithNewPeriodAndAssignmentAsync(
        Employee employee,
        EmploymentPeriod newPeriod,
        EmployeeOrganizationAssignment newAssignment,
        CancellationToken cancellationToken = default)
    {
        ValidateTriple(
            employee,
            newPeriod,
            newAssignment);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        dbContext.Employees.Update(
            employee);

        dbContext.EmploymentPeriods.Add(
            newPeriod);

        dbContext.EmployeeOrganizationAssignments.Add(
            newAssignment);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }

    private static void ValidateTriple(
        Employee employee,
        EmploymentPeriod period,
        EmployeeOrganizationAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(employee);
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(assignment);

        if (period.EmployeeId != employee.Id)
        {
            throw new ArgumentException(
                "Giai đoạn làm việc không thuộc nhân viên được cung cấp.",
                nameof(period));
        }

        if (assignment.EmployeeId != employee.Id)
        {
            throw new ArgumentException(
                "Phân công không thuộc nhân viên được cung cấp.",
                nameof(assignment));
        }

        if (assignment.EmploymentPeriodId != period.Id)
        {
            throw new ArgumentException(
                "Phân công không thuộc giai đoạn làm việc được cung cấp.",
                nameof(assignment));
        }

        if (!employee.DepartmentId.HasValue || !employee.PositionId.HasValue)
        {
            throw new ArgumentException(
                "Nhân viên chưa có đầy đủ thông tin tổ chức.",
                nameof(employee));
        }

        if (assignment.DepartmentId != employee.DepartmentId.Value
            || assignment.PositionId != employee.PositionId.Value)
        {
            throw new ArgumentException(
                "Phân công không khớp với tổ chức hiện tại của nhân viên.",
                nameof(assignment));
        }
    }
}
