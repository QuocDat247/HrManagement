using System.Data;
using HrManagement.Application.Leave.Requests;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Leave.Policies;
using HrManagement.Domain.Leave.Requests;
using HrManagement.Domain.Leave.Types;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Leave.Requests;

public sealed class EfLeaveRequestSubmissionPersistence
    : ILeaveRequestSubmissionPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfLeaveRequestSubmissionPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task SubmitAsync(
        LeaveRequest leaveRequest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            leaveRequest);

        if (leaveRequest.Status !=
            LeaveRequestStatus.Pending)
        {
            throw new ArgumentException(
                "Chỉ đơn nghỉ phép đang chờ duyệt mới có thể được gửi.",
                nameof(leaveRequest));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await using var transaction =
            await dbContext.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

        EmploymentPeriod? employmentPeriod =
            await dbContext
                .EmploymentPeriods
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    period =>
                        period.Id ==
                        leaveRequest.EmploymentPeriodId,
                    cancellationToken);

        if (employmentPeriod is null)
        {
            throw new DbUpdateConcurrencyException(
                "Giai đoạn làm việc không còn tồn tại.");
        }

        LeaveType? leaveType =
            await dbContext
                .LeaveTypes
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    type =>
                        type.Id ==
                        leaveRequest.LeaveTypeId,
                    cancellationToken);

        if (leaveType is null)
        {
            throw new DbUpdateConcurrencyException(
                "Loại nghỉ phép không còn tồn tại.");
        }

        try
        {
            LeaveRequestEligibilityPolicy
                .EnsureCanRequest(
                    leaveRequest.EmployeeId,
                    employmentPeriod,
                    leaveType,
                    leaveRequest.StartDate,
                    leaveRequest.EndDate);
        }
        catch (ArgumentException exception)
        {
            throw new DbUpdateConcurrencyException(
                "Điều kiện gửi đơn nghỉ phép đã thay đổi.",
                exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new DbUpdateConcurrencyException(
                "Điều kiện gửi đơn nghỉ phép đã thay đổi.",
                exception);
        }

        IReadOnlyList<LeaveRequest> persistedOverlaps =
            await dbContext
                .LeaveRequests
                .AsNoTracking()
                .Where(
                    request =>
                        request.EmployeeId ==
                            leaveRequest.EmployeeId
                        && request.StartDate <=
                            leaveRequest.EndDate
                        && leaveRequest.StartDate <=
                            request.EndDate)
                .OrderBy(
                    request =>
                        request.StartDate)
                .ThenBy(
                    request =>
                        request.EndDate)
                .ThenBy(
                    request =>
                        request.Id)
                .ToListAsync(
                    cancellationToken);

        try
        {
            LeaveRequestOverlapPolicy
                .EnsureNoOverlap(
                    leaveRequest.EmployeeId,
                    leaveRequest.StartDate,
                    leaveRequest.EndDate,
                    persistedOverlaps);
        }
        catch (InvalidOperationException exception)
        {
            throw new DbUpdateConcurrencyException(
                "Đã xuất hiện một đơn nghỉ phép đang có hiệu lực trùng khoảng ngày trước khi lưu.",
                exception);
        }

        await dbContext
            .LeaveRequests
            .AddAsync(
                leaveRequest,
                cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }
}
