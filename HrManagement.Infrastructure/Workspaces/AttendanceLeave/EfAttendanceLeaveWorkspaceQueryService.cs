using HrManagement.Application.Workspaces.AttendanceLeave;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Domain.Leave.Requests;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Workspaces.AttendanceLeave;

public sealed class EfAttendanceLeaveWorkspaceQueryService
    : IAttendanceLeaveWorkspaceQueryService
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfAttendanceLeaveWorkspaceQueryService(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<AttendanceLeaveWorkspaceSnapshot> GetAsync(
        AttendanceLeaveWorkspaceQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            query);

        if (query.FromDate == default)
        {
            throw new ArgumentException(
                "Ngày bắt đầu tra cứu không hợp lệ.",
                nameof(query));
        }

        if (query.ToDate == default)
        {
            throw new ArgumentException(
                "Ngày kết thúc tra cứu không hợp lệ.",
                nameof(query));
        }

        if (query.ToDate <
            query.FromDate)
        {
            throw new ArgumentException(
                "Ngày kết thúc tra cứu không thể trước ngày bắt đầu.",
                nameof(query));
        }

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

        IQueryable<AttendanceRecord> attendanceRecords =
            dbContext
                .AttendanceRecords
                .AsNoTracking()
                .Where(
                    record =>
                        record.WorkDate >=
                            query.FromDate
                        && record.WorkDate <=
                            query.ToDate);

        if (query.EmployeeId.HasValue)
        {
            Guid employeeId =
                query.EmployeeId.Value;

            attendanceRecords =
                attendanceRecords.Where(
                    record =>
                        record.EmployeeId ==
                        employeeId);
        }

        List<AttendanceWorkspaceItem> attendance =
            await (
                from record in attendanceRecords

                join employee
                    in dbContext.Employees.AsNoTracking()
                    on record.EmployeeId
                    equals employee.Id

                orderby
                    record.WorkDate descending,
                    employee.EmployeeCode

                select new AttendanceWorkspaceItem(
                    record.Id,
                    employee.Id,
                    employee.EmployeeCode,
                    employee.FullName,
                    record.WorkDate,
                    record.IsWorkingDay,
                    record.ExpectedStartTime,
                    record.ExpectedEndTime,
                    record.Status,
                    record.WorkedMinutes,
                    record.LateMinutes,
                    record.EarlyLeaveMinutes)
            )
            .ToListAsync(
                cancellationToken);

        IQueryable<LeaveRequest> leaveRequests =
            dbContext
                .LeaveRequests
                .AsNoTracking()
                .Where(
                    request =>
                        request.StartDate <=
                            query.ToDate
                        && query.FromDate <=
                            request.EndDate);

        if (query.EmployeeId.HasValue)
        {
            Guid employeeId =
                query.EmployeeId.Value;

            leaveRequests =
                leaveRequests.Where(
                    request =>
                        request.EmployeeId ==
                        employeeId);
        }

        List<LeaveWorkspaceItem> leave =
            await (
                from request in leaveRequests

                join employee
                    in dbContext.Employees.AsNoTracking()
                    on request.EmployeeId
                    equals employee.Id

                join leaveType
                    in dbContext.LeaveTypes.AsNoTracking()
                    on request.LeaveTypeId
                    equals leaveType.Id

                orderby
                    request.StartDate descending,
                    request.SubmittedAtUtc descending,
                    employee.EmployeeCode

                select new LeaveWorkspaceItem(
                    request.Id,
                    employee.Id,
                    employee.EmployeeCode,
                    employee.FullName,
                    leaveType.Id,
                    leaveType.Code,
                    leaveType.Name,
                    leaveType.IsPaid,
                    request.StartDate,
                    request.EndDate,
                    request.Status,
                    request.SubmittedAtUtc,
                    request.Reason)
            )
            .ToListAsync(
                cancellationToken);

        return new AttendanceLeaveWorkspaceSnapshot(
            attendance,
            leave);
    }

    public async Task<IReadOnlyList<AttendanceLeaveEmployeeItem>>
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
                    new AttendanceLeaveEmployeeItem(
                        employee.Id,
                        employee.EmployeeCode,
                        employee.FullName))
            .ToListAsync(
                cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveTypeWorkspaceOption>>
    GetActiveLeaveTypesAsync(
        CancellationToken cancellationToken = default)
    {
        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .LeaveTypes
            .AsNoTracking()
            .Where(
                leaveType =>
                    leaveType.IsActive)
            .OrderBy(
                leaveType =>
                    leaveType.Code)
            .ThenBy(
                leaveType =>
                    leaveType.Name)
            .ThenBy(
                leaveType =>
                    leaveType.Id)
            .Select(
                leaveType =>
                    new LeaveTypeWorkspaceOption(
                        leaveType.Id,
                        leaveType.Code,
                        leaveType.Name,
                        leaveType.IsPaid))
            .ToListAsync(
                cancellationToken);
    }
}
