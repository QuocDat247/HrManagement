using System.Data;
using HrManagement.Application.Auditing;
using HrManagement.Application.Overtime.Requests;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Overtime.Requests;
using HrManagement.Infrastructure.Attendance.Timesheets;
using HrManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Overtime.Requests;

public sealed class EfOvertimeRequestSubmissionPersistence
    : IOvertimeRequestSubmissionPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    private readonly IAuditEntryFactory
        _auditEntryFactory;

    public EfOvertimeRequestSubmissionPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory,
        IAuditEntryFactory auditEntryFactory)
    {
        _dbContextFactory =
            dbContextFactory;

        _auditEntryFactory =
            auditEntryFactory;
    }

    public async Task SubmitAsync(
        OvertimeRequest overtimeRequest,
        string actorUserId,
        string actorUsername,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            overtimeRequest);

        if (overtimeRequest.Status !=
            OvertimeRequestStatus.Pending)
        {
            throw new ArgumentException(
                "Chỉ yêu cầu tăng ca đang chờ duyệt mới có thể được gửi.",
                nameof(overtimeRequest));
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

        bool employeeExists =
            await dbContext
                .Employees
                .AsNoTracking()
                .AnyAsync(
                    employee =>
                        employee.Id ==
                        overtimeRequest.EmployeeId,
                    cancellationToken);

        if (!employeeExists)
        {
            throw new DbUpdateConcurrencyException(
                "Nhân viên không còn tồn tại.");
        }

        EmploymentPeriod? employmentPeriod =
            await dbContext
                .EmploymentPeriods
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    period =>
                        period.Id ==
                        overtimeRequest.EmploymentPeriodId,
                    cancellationToken);

        if (employmentPeriod is null)
        {
            throw new DbUpdateConcurrencyException(
                "Giai đoạn làm việc không còn tồn tại.");
        }

        if (employmentPeriod.EmployeeId !=
            overtimeRequest.EmployeeId)
        {
            throw new DbUpdateConcurrencyException(
                "Giai đoạn làm việc không thuộc nhân viên của yêu cầu tăng ca.");
        }

        if (overtimeRequest.WorkDate <
                employmentPeriod.StartDate
            || (
                employmentPeriod.EndDate.HasValue
                && overtimeRequest.WorkDate >
                    employmentPeriod.EndDate.Value
            ))
        {
            throw new DbUpdateConcurrencyException(
                "Ngày tăng ca không còn nằm trong giai đoạn làm việc của nhân viên.");
        }

        try
        {
            await AttendancePeriodWriteGuard
                .EnsureUnlockedAsync(
                    dbContext,
                    overtimeRequest.WorkDate,
                    cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidOperationException(
                "Kỳ công của ngày tăng ca đã được đóng. Không thể gửi yêu cầu tăng ca.",
                exception);
        }

        bool activeRequestExists =
            await dbContext
                .OvertimeRequests
                .AsNoTracking()
                .AnyAsync(
                    existing =>
                        existing.EmployeeId ==
                            overtimeRequest.EmployeeId
                        && existing.WorkDate ==
                            overtimeRequest.WorkDate
                        && (
                            existing.Status ==
                                OvertimeRequestStatus.Pending
                            || existing.Status ==
                                OvertimeRequestStatus.Approved
                        ),
                    cancellationToken);

        if (activeRequestExists)
        {
            throw new InvalidOperationException(
                "Nhân viên đã có một yêu cầu tăng ca đang có hiệu lực trong ngày này.");
        }

        AuditEntry auditEntry =
            _auditEntryFactory.Create(
                AuditAction.Created,
                AuditEntityTypes.OvertimeRequest,
                overtimeRequest.Id,
                overtimeRequest.EmployeeId);

        if (!string.Equals(
                auditEntry.ActorUserId,
                actorUserId,
                StringComparison.Ordinal)
            || !string.Equals(
                auditEntry.ActorUsername,
                actorUsername,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Người gửi yêu cầu tăng ca không khớp với người dùng audit hiện tại.");
        }

        await dbContext
            .OvertimeRequests
            .AddAsync(
                overtimeRequest,
                cancellationToken);

        await dbContext
            .AuditEntries
            .AddAsync(
                auditEntry,
                cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsActiveRequestUniqueConstraintViolation(
                exception))
        {
            throw new InvalidOperationException(
                "Nhân viên đã có một yêu cầu tăng ca đang có hiệu lực trong ngày này.",
                exception);
        }

        await transaction.CommitAsync(
            cancellationToken);
    }

    private static bool
        IsActiveRequestUniqueConstraintViolation(
            DbUpdateException exception)
    {
        if (exception.InnerException
            is not SqliteException sqliteException)
        {
            return false;
        }

        const int SqliteConstraint =
            19;

        const int SqliteConstraintUnique =
            2067;

        return
            sqliteException.SqliteErrorCode ==
                SqliteConstraint
            && sqliteException.SqliteExtendedErrorCode ==
                SqliteConstraintUnique
            && sqliteException.Message.Contains(
                "OvertimeRequests.EmployeeId",
                StringComparison.OrdinalIgnoreCase)
            && sqliteException.Message.Contains(
                "OvertimeRequests.WorkDate",
                StringComparison.OrdinalIgnoreCase);
    }
}
