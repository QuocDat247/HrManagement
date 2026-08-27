using HrManagement.Application.Workspaces.Overtime;
using HrManagement.Domain.Overtime.Requests;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Workspaces.Overtime;

public sealed class EfOvertimeWorkspaceQueryService
    : IOvertimeWorkspaceQueryService
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfOvertimeWorkspaceQueryService(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<OvertimeWorkspaceSnapshot> GetAsync(
        OvertimeWorkspaceQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            query);

        ValidateQuery(
            query);

        DateOnly fromDate =
            new(
                query.Year,
                query.Month,
                1);

        DateOnly toDate =
            new(
                query.Year,
                query.Month,
                DateTime.DaysInMonth(
                    query.Year,
                    query.Month));

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        IQueryable<OvertimeRequest> requests =
            dbContext
                .OvertimeRequests
                .AsNoTracking()
                .Where(
                    request =>
                        request.WorkDate >=
                            fromDate
                        && request.WorkDate <=
                            toDate);

        if (query.EmployeeId.HasValue)
        {
            Guid employeeId =
                query.EmployeeId.Value;

            requests =
                requests.Where(
                    request =>
                        request.EmployeeId ==
                        employeeId);
        }

        if (query.Status.HasValue)
        {
            OvertimeRequestStatus status =
                query.Status.Value;

            requests =
                requests.Where(
                    request =>
                        request.Status ==
                        status);
        }

        List<OvertimeWorkspaceItem> items =
            await (
                from request in requests

                join employee
                    in dbContext
                        .Employees
                        .AsNoTracking()
                    on request.EmployeeId
                    equals employee.Id

                orderby
                    request.WorkDate descending,
                    request.SubmittedAtUtc descending,
                    employee.EmployeeCode,
                    request.Id

                select new OvertimeWorkspaceItem(
                    request.Id,
                    employee.Id,
                    employee.EmployeeCode,
                    employee.FullName,
                    request.WorkDate,
                    request.RequestedMinutes,
                    request.ApprovedMinutes,
                    request.Status,
                    request.SubmittedAtUtc,
                    request.Reason)
            )
            .ToListAsync(
                cancellationToken);

        return new OvertimeWorkspaceSnapshot(
            items);
    }

    public async Task<IReadOnlyList<OvertimeEmployeeOption>>
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
                    new OvertimeEmployeeOption(
                        employee.Id,
                        employee.EmployeeCode,
                        employee.FullName))
            .ToListAsync(
                cancellationToken);
    }

    public async Task<IReadOnlyList<OvertimeStatusHistoryItem>>
        GetHistoryAsync(
            Guid overtimeRequestId,
            CancellationToken cancellationToken = default)
    {
        if (overtimeRequestId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã yêu cầu tăng ca không hợp lệ.",
                nameof(overtimeRequestId));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        bool requestExists =
            await dbContext
                .OvertimeRequests
                .AsNoTracking()
                .AnyAsync(
                    request =>
                        request.Id ==
                        overtimeRequestId,
                    cancellationToken);

        if (!requestExists)
        {
            return
                Array.Empty<OvertimeStatusHistoryItem>();
        }

        return await dbContext
            .OvertimeRequestStatusChanges
            .AsNoTracking()
            .Where(
                change =>
                    change.OvertimeRequestId ==
                    overtimeRequestId)
            .OrderByDescending(
                change =>
                    change.ChangedAtUtc)
            .ThenByDescending(
                change =>
                    change.Id)
            .Select(
                change =>
                    new OvertimeStatusHistoryItem(
                        change.Id,
                        change.OvertimeRequestId,
                        change.PreviousStatus,
                        change.NewStatus,
                        change.ApprovedMinutes,
                        change.ChangedAtUtc,
                        change.ChangedByUsername,
                        change.Note))
            .ToListAsync(
                cancellationToken);
    }

    private static void ValidateQuery(
        OvertimeWorkspaceQuery query)
    {
        if (query.Year < 2000
            || query.Year > 9999)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                "Năm tra cứu tăng ca không hợp lệ.");
        }

        if (query.Month < 1
            || query.Month > 12)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                "Tháng tra cứu tăng ca phải từ 1 đến 12.");
        }

        if (query.EmployeeId ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên tra cứu không hợp lệ.",
                nameof(query));
        }

        if (query.Status.HasValue
            && !Enum.IsDefined(
                query.Status.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                "Trạng thái tăng ca tra cứu không hợp lệ.");
        }
    }
}
