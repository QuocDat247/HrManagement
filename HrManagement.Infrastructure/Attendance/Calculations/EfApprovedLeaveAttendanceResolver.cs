using HrManagement.Application.Attendance.Calculations;
using HrManagement.Domain.Leave.Requests;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Calculations;

public sealed class EfApprovedLeaveAttendanceResolver
    : IApprovedLeaveAttendanceResolver
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfApprovedLeaveAttendanceResolver(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<ApprovedLeaveAttendanceInput?> ResolveAsync(
        Guid employeeId,
        Guid employmentPeriodId,
        DateOnly workDate,
        CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty
            || employmentPeriodId == Guid.Empty
            || workDate == default)
        {
            return null;
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        List<ApprovedLeaveAttendanceInput> matches =
            await dbContext
                .LeaveRequests
                .AsNoTracking()
                .Where(
                    request =>
                        request.EmployeeId ==
                            employeeId
                        && request.EmploymentPeriodId ==
                            employmentPeriodId
                        && request.Status ==
                            LeaveRequestStatus.Approved
                        && request.StartDate <=
                            workDate
                        && workDate <=
                            request.EndDate)
                .OrderBy(
                    request =>
                        request.StartDate)
                .ThenBy(
                    request =>
                        request.SubmittedAtUtc)
                .ThenBy(
                    request =>
                        request.Id)
                .Select(
                    request =>
                        new ApprovedLeaveAttendanceInput(
                            request.Id,
                            request.LeaveTypeId))
                .Take(2)
                .ToListAsync(
                    cancellationToken);

        if (matches.Count > 1)
        {
            throw new InvalidOperationException(
                "Có nhiều đơn nghỉ phép đã duyệt cùng áp dụng cho ngày chấm công.");
        }

        return matches.Count == 1
            ? matches[0]
            : null;
    }
}
