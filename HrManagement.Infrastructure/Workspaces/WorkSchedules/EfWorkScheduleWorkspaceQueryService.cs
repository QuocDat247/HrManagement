using HrManagement.Application.Workspaces.WorkSchedules;
using HrManagement.Domain.Attendance.Schedules;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Workspaces.WorkSchedules;

public sealed class EfWorkScheduleWorkspaceQueryService
    : IWorkScheduleWorkspaceQueryService
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfWorkScheduleWorkspaceQueryService(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<IReadOnlyList<WorkScheduleWorkspaceEmployeeItem>>
        GetEmployeesAsync(
            CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .Employees
            .AsNoTracking()
            .OrderBy(
                employee =>
                    employee.EmployeeCode)
            .ThenBy(
                employee =>
                    employee.FullName)
            .ThenBy(
                employee =>
                    employee.Id)
            .Select(
                employee =>
                    new WorkScheduleWorkspaceEmployeeItem(
                        employee.Id,
                        employee.EmployeeCode,
                        employee.FullName))
            .ToListAsync(
                cancellationToken);
    }

    public async Task<WorkScheduleWorkspaceSnapshot> GetAsync(
        WorkScheduleWorkspaceQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            query);

        if (query.EmployeeId ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên tra cứu không hợp lệ.",
                nameof(query));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        List<WorkScheduleWorkspaceScheduleItem> schedules =
            await dbContext
                .WorkSchedules
                .AsNoTracking()
                .OrderBy(
                    schedule =>
                        schedule.Code)
                .ThenBy(
                    schedule =>
                        schedule.Name)
                .ThenBy(
                    schedule =>
                        schedule.Id)
                .Select(
                    schedule =>
                        new WorkScheduleWorkspaceScheduleItem(
                            schedule.Id,
                            schedule.Code,
                            schedule.Name,
                            schedule.TimeZoneId,
                            schedule.IsActive))
                .ToListAsync(
                    cancellationToken);

        List<WorkScheduleWorkspaceDayItem> scheduleDays =
            await dbContext
                .WorkScheduleDays
                .AsNoTracking()
                .OrderBy(
                    day =>
                        day.WorkScheduleId)
                .ThenBy(
                    day =>
                        day.DayOfWeek)
                .ThenBy(
                    day =>
                        day.Id)
                .Select(
                    day =>
                        new WorkScheduleWorkspaceDayItem(
                            day.Id,
                            day.WorkScheduleId,
                            day.DayOfWeek,
                            day.IsWorkingDay,
                            day.StartTime,
                            day.EndTime,
                            day.BreakMinutes,
                            day.PlannedMinutes))
                .ToListAsync(
                    cancellationToken);

        IQueryable<EmployeeWorkScheduleAssignment> assignments =
            dbContext
                .EmployeeWorkScheduleAssignments
                .AsNoTracking();

        if (query.EmployeeId.HasValue)
        {
            Guid employeeId =
                query.EmployeeId.Value;

            assignments =
                assignments.Where(
                    assignment =>
                        assignment.EmployeeId ==
                        employeeId);
        }

        List<WorkScheduleWorkspaceAssignmentItem> assignmentItems =
            await (
                from assignment in assignments

                join employee
                    in dbContext.Employees.AsNoTracking()
                    on assignment.EmployeeId
                    equals employee.Id

                join schedule
                    in dbContext.WorkSchedules.AsNoTracking()
                    on assignment.WorkScheduleId
                    equals schedule.Id

                orderby
                    employee.EmployeeCode,
                    assignment.EffectiveFrom descending,
                    assignment.Id

                select new WorkScheduleWorkspaceAssignmentItem(
                    assignment.Id,
                    employee.Id,
                    employee.EmployeeCode,
                    employee.FullName,
                    assignment.EmploymentPeriodId,
                    schedule.Id,
                    schedule.Code,
                    schedule.Name,
                    assignment.EffectiveFrom,
                    assignment.EffectiveTo,
                    assignment.EffectiveTo == null)
            )
            .ToListAsync(
                cancellationToken);

        return new WorkScheduleWorkspaceSnapshot(
            schedules,
            scheduleDays,
            assignmentItems);
    }
}
