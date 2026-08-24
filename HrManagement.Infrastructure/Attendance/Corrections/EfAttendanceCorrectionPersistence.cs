using System.Data;
using HrManagement.Application.Attendance.Corrections;
using HrManagement.Application.Auditing;
using HrManagement.Domain.Attendance.Corrections;
using HrManagement.Domain.Auditing;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Corrections;

public sealed class EfAttendanceCorrectionPersistence
    : IAttendanceCorrectionPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    private readonly IAuditEntryFactory
        _auditEntryFactory;

    public EfAttendanceCorrectionPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory,
        IAuditEntryFactory auditEntryFactory)
    {
        _dbContextFactory =
            dbContextFactory;

        _auditEntryFactory =
            auditEntryFactory;
    }

    public async Task<IReadOnlyList<AttendanceCorrection>>
        GetByAttendanceRecordIdAsync(
            Guid attendanceRecordId,
            CancellationToken cancellationToken = default)
    {
        if (attendanceRecordId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã bản ghi chấm công không hợp lệ.",
                nameof(attendanceRecordId));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .AttendanceCorrections
            .AsNoTracking()
            .Where(
                correction =>
                    correction.AttendanceRecordId ==
                    attendanceRecordId)
            .OrderBy(
                correction =>
                    correction.Revision)
            .ToArrayAsync(
                cancellationToken);
    }

    public async Task AppendAsync(
        AttendanceCorrection correction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            correction);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await using var transaction =
            await dbContext.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

        var attendanceRecord =
            await dbContext
                .AttendanceRecords
                .AsNoTracking()
                .Where(
                    record =>
                        record.Id ==
                        correction.AttendanceRecordId)
                .Select(
                    record =>
                        new
                        {
                            record.EmployeeId
                        })
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (attendanceRecord is null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy bản ghi chấm công cần điều chỉnh.");
        }

        if (attendanceRecord.EmployeeId !=
            correction.EmployeeId)
        {
            throw new InvalidOperationException(
                "Điều chỉnh chấm công không thuộc nhân viên của bản ghi chấm công.");
        }

        int latestRevision =
            await dbContext
                .AttendanceCorrections
                .Where(
                    existing =>
                        existing.AttendanceRecordId ==
                        correction.AttendanceRecordId)
                .Select(
                    existing =>
                        (int?)existing.Revision)
                .MaxAsync(
                    cancellationToken)
            ?? 0;

        int expectedRevision =
            latestRevision + 1;

        if (correction.Revision !=
            expectedRevision)
        {
            throw new InvalidOperationException(
                "Phiên bản điều chỉnh chấm công phải là phiên bản kế tiếp.");
        }

        AuditEntry auditEntry =
            _auditEntryFactory.Create(
                AuditAction.Created,
                AuditEntityTypes.AttendanceCorrection,
                correction.Id,
                correction.EmployeeId);

        if (!string.Equals(
                auditEntry.ActorUserId,
                correction.ActorUserId,
                StringComparison.Ordinal)
            || !string.Equals(
                auditEntry.ActorUsername,
                correction.ActorUsername,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Người thực hiện correction không khớp với người dùng audit hiện tại.");
        }

        await dbContext
            .AttendanceCorrections
            .AddAsync(
                correction,
                cancellationToken);

        await dbContext
            .AuditEntries
            .AddAsync(
                auditEntry,
                cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }
}
