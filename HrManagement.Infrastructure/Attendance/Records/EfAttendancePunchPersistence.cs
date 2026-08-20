using HrManagement.Application.Attendance.Records;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Attendance.Records;

public sealed class EfAttendancePunchPersistence
    : IAttendancePunchPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfAttendancePunchPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task AppendAsync(
        AttendanceRecord? newRecord,
        AttendanceEvent newEvent,
        AttendanceEvent? expectedLastEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            newEvent);

        ValidateRequest(
            newRecord,
            newEvent,
            expectedLastEvent);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await using var transaction =
            await dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        if (newRecord is not null)
        {
            await AppendFirstEventAsync(
                dbContext,
                newRecord,
                newEvent,
                cancellationToken);
        }
        else
        {
            await AppendToExistingRecordAsync(
                dbContext,
                newEvent,
                expectedLastEvent,
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }

    private static async Task AppendFirstEventAsync(
        HrManagementDbContext dbContext,
        AttendanceRecord newRecord,
        AttendanceEvent newEvent,
        CancellationToken cancellationToken)
    {
        bool recordAlreadyExists =
            await dbContext
                .AttendanceRecords
                .AsNoTracking()
                .AnyAsync(
                    record =>
                        record.Id ==
                            newRecord.Id
                        || (
                            record.EmployeeId ==
                                newRecord.EmployeeId
                            && record.WorkDate ==
                                newRecord.WorkDate
                        ),
                    cancellationToken);

        if (recordAlreadyExists)
        {
            throw new DbUpdateConcurrencyException(
                "Bản ghi chấm công cho ngày làm việc này đã được tạo.");
        }

        AttendancePunchSequencePolicy
            .EnsureCanAppend(
                [],
                newEvent.EventType,
                newEvent.OccurredAtUtc);

        await dbContext
            .AttendanceRecords
            .AddAsync(
                newRecord,
                cancellationToken);

        await dbContext
            .AttendanceEvents
            .AddAsync(
                newEvent,
                cancellationToken);
    }

    private static async Task AppendToExistingRecordAsync(
        HrManagementDbContext dbContext,
        AttendanceEvent newEvent,
        AttendanceEvent? expectedLastEvent,
        CancellationToken cancellationToken)
    {
        AttendanceRecord? persistedRecord =
            await dbContext
                .AttendanceRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    record =>
                        record.Id ==
                        newEvent.AttendanceRecordId,
                    cancellationToken);

        if (persistedRecord is null)
        {
            throw new DbUpdateConcurrencyException(
                "Không còn tìm thấy bản ghi chấm công.");
        }

        if (persistedRecord.EmployeeId !=
            newEvent.EmployeeId)
        {
            throw new ArgumentException(
                "Sự kiện chấm công không thuộc nhân viên của bản ghi.",
                nameof(newEvent));
        }

        IReadOnlyList<AttendanceEvent> persistedEvents =
            await dbContext
                .AttendanceEvents
                .AsNoTracking()
                .Where(
                    attendanceEvent =>
                        attendanceEvent.AttendanceRecordId ==
                        persistedRecord.Id)
                .OrderBy(
                    attendanceEvent =>
                        attendanceEvent.OccurredAtUtc)
                .ThenBy(
                    attendanceEvent =>
                        attendanceEvent.Id)
                .ToListAsync(
                    cancellationToken);

        ValidateExpectedLastEvent(
            persistedEvents,
            expectedLastEvent);

        AttendancePunchSequencePolicy
            .EnsureCanAppend(
                persistedEvents,
                newEvent.EventType,
                newEvent.OccurredAtUtc);

        await dbContext
            .AttendanceEvents
            .AddAsync(
                newEvent,
                cancellationToken);
    }

    private static void ValidateRequest(
        AttendanceRecord? newRecord,
        AttendanceEvent newEvent,
        AttendanceEvent? expectedLastEvent)
    {
        if (newRecord is not null)
        {
            if (expectedLastEvent is not null)
            {
                throw new ArgumentException(
                    "Bản ghi chấm công mới không được có sự kiện trước đó.",
                    nameof(expectedLastEvent));
            }

            if (newEvent.AttendanceRecordId !=
                newRecord.Id)
            {
                throw new ArgumentException(
                    "Sự kiện chấm công phải thuộc bản ghi mới.",
                    nameof(newEvent));
            }

            if (newEvent.EmployeeId !=
                newRecord.EmployeeId)
            {
                throw new ArgumentException(
                    "Sự kiện chấm công phải thuộc cùng nhân viên với bản ghi.",
                    nameof(newEvent));
            }

            return;
        }

        if (expectedLastEvent is null)
        {
            return;
        }

        if (expectedLastEvent.AttendanceRecordId !=
            newEvent.AttendanceRecordId)
        {
            throw new ArgumentException(
                "Sự kiện dự kiến gần nhất không thuộc cùng bản ghi chấm công.",
                nameof(expectedLastEvent));
        }

        if (expectedLastEvent.EmployeeId !=
            newEvent.EmployeeId)
        {
            throw new ArgumentException(
                "Sự kiện dự kiến gần nhất không thuộc cùng nhân viên.",
                nameof(expectedLastEvent));
        }
    }

    private static void ValidateExpectedLastEvent(
        IReadOnlyList<AttendanceEvent> persistedEvents,
        AttendanceEvent? expectedLastEvent)
    {
        if (expectedLastEvent is null)
        {
            if (persistedEvents.Count != 0)
            {
                throw new DbUpdateConcurrencyException(
                    "Lịch sử chấm công đã thay đổi trước khi ghi sự kiện.");
            }

            return;
        }

        if (persistedEvents.Count == 0)
        {
            throw new DbUpdateConcurrencyException(
                "Sự kiện chấm công gần nhất không còn tồn tại.");
        }

        AttendanceEvent actualLastEvent =
            persistedEvents[
                persistedEvents.Count - 1];

        if (actualLastEvent.Id !=
                expectedLastEvent.Id
            || actualLastEvent.AttendanceRecordId !=
                expectedLastEvent.AttendanceRecordId
            || actualLastEvent.EmployeeId !=
                expectedLastEvent.EmployeeId
            || actualLastEvent.EventType !=
                expectedLastEvent.EventType
            || actualLastEvent.OccurredAtUtc !=
                expectedLastEvent.OccurredAtUtc)
        {
            throw new DbUpdateConcurrencyException(
                "Lịch sử chấm công đã thay đổi trước khi ghi sự kiện.");
        }
    }
}
