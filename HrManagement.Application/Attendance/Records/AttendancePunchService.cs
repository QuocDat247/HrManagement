using HrManagement.Domain.Attendance.Records;
using HrManagement.Application.Attendance.Timesheets;

namespace HrManagement.Application.Attendance.Records;

public sealed class AttendancePunchService
    : IAttendancePunchService
{
    private readonly IAttendanceRecordRepository
        _recordRepository;

    private readonly IAttendanceEventRepository
        _eventRepository;

    private readonly IAttendancePunchContextResolver
        _contextResolver;

    private readonly IAttendancePunchPersistence
        _persistence;

    private readonly IAttendancePeriodLockPolicy
        _periodLockPolicy;

    public AttendancePunchService(
        IAttendanceRecordRepository recordRepository,
        IAttendanceEventRepository eventRepository,
        IAttendancePunchContextResolver contextResolver,
        IAttendancePunchPersistence persistence,
        IAttendancePeriodLockPolicy periodLockPolicy)
    {
        _recordRepository =
            recordRepository;

        _eventRepository =
            eventRepository;

        _contextResolver =
            contextResolver;

        _persistence =
            persistence;

        _periodLockPolicy =
            periodLockPolicy;
    }

    public async Task<RecordAttendancePunchResult> RecordAsync(
        RecordAttendancePunchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EmployeeId == Guid.Empty)
        {
            return Failure(
                "Mã nhân viên không hợp lệ.");
        }

        if (!Enum.IsDefined(
                request.EventType))
        {
            return Failure(
                "Loại sự kiện chấm công không hợp lệ.");
        }

        if (request.OccurredAtUtc == default)
        {
            return Failure(
                "Thời điểm chấm công không hợp lệ.");
        }

        if (request.OccurredAtUtc.Kind !=
            DateTimeKind.Utc)
        {
            return Failure(
                "Thời điểm chấm công phải được lưu theo UTC.");
        }

        AttendanceEvent? latestEvent =
            await _eventRepository
                .GetLatestByEmployeeIdAsync(
                    request.EmployeeId,
                    cancellationToken);

        if (latestEvent is not null
            && latestEvent.EmployeeId !=
                request.EmployeeId)
        {
            return Failure(
                "Sự kiện chấm công gần nhất không thuộc nhân viên.");
        }

        if (latestEvent?.EventType ==
            AttendanceEventType.ClockIn)
        {
            return await AppendToOpenRecordAsync(
                request,
                latestEvent,
                cancellationToken);
        }

        if (request.EventType ==
            AttendanceEventType.ClockOut)
        {
            return Failure(
                "Không có ClockIn đang mở để thực hiện ClockOut.");
        }

        return await AppendClockInAsync(
            request,
            cancellationToken);
    }

    private async Task<RecordAttendancePunchResult>
        AppendToOpenRecordAsync(
            RecordAttendancePunchRequest request,
            AttendanceEvent latestEvent,
            CancellationToken cancellationToken)
    {
        AttendanceRecord? record =
            await _recordRepository
                .GetByIdAsync(
                    latestEvent.AttendanceRecordId,
                    cancellationToken);

        if (record is null)
        {
            return Failure(
                "Không tìm thấy bản ghi chấm công của ClockIn đang mở.");
        }

        if (record.EmployeeId !=
            request.EmployeeId)
        {
            return Failure(
                "Bản ghi chấm công không thuộc nhân viên.");
        }

        bool isPeriodLocked =
            await _periodLockPolicy
                .IsLockedAsync(
                    record.WorkDate,
                    cancellationToken);

        if (isPeriodLocked)
        {
            return Failure(
                "Kỳ công của ngày chấm công đã được đóng. Không thể ghi nhận chấm công.");
        }

        IReadOnlyList<AttendanceEvent> existingEvents =
            await _eventRepository
                .GetByAttendanceRecordIdAsync(
                    record.Id,
                    cancellationToken);

        if (existingEvents.Count == 0)
        {
            return Failure(
                "Bản ghi chấm công đang mở không có lịch sử sự kiện.");
        }

        AttendanceEvent actualLastEvent =
            existingEvents[
                existingEvents.Count - 1];

        if (actualLastEvent.Id !=
            latestEvent.Id)
        {
            return Failure(
                "Lịch sử chấm công đã thay đổi, vui lòng thử lại.");
        }

        return await AppendToExistingRecordAsync(
            record,
            existingEvents,
            request,
            cancellationToken);
    }

    private async Task<RecordAttendancePunchResult>
        AppendClockInAsync(
            RecordAttendancePunchRequest request,
            CancellationToken cancellationToken)
    {
        AttendancePunchContextResolutionResult resolution =
            await _contextResolver
                .ResolveAsync(
                    request.EmployeeId,
                    request.OccurredAtUtc,
                    cancellationToken);

        if (!resolution.IsSuccessful
            || resolution.Context is null)
        {
            return Failure(
                resolution.ErrorMessage
                ?? "Không thể xác định lịch làm việc cho thời điểm chấm công.");
        }

        AttendancePunchContext context =
            resolution.Context;

        if (context.EmployeeId !=
            request.EmployeeId)
        {
            return Failure(
                "Ngữ cảnh chấm công không thuộc nhân viên.");
        }

        bool isPeriodLocked =
            await _periodLockPolicy
                .IsLockedAsync(
                    context.WorkDate,
                    cancellationToken);

        if (isPeriodLocked)
        {
            return Failure(
                "Kỳ công của ngày chấm công đã được đóng. Không thể ghi nhận chấm công.");
        }

        AttendanceRecord? existingRecord =
            await _recordRepository
                .GetByEmployeeAndWorkDateAsync(
                    request.EmployeeId,
                    context.WorkDate,
                    cancellationToken);

        if (existingRecord is not null)
        {
            if (existingRecord.EmployeeId !=
                request.EmployeeId)
            {
                return Failure(
                    "Bản ghi chấm công không thuộc nhân viên.");
            }

            IReadOnlyList<AttendanceEvent> existingEvents =
                await _eventRepository
                    .GetByAttendanceRecordIdAsync(
                        existingRecord.Id,
                        cancellationToken);

            return await AppendToExistingRecordAsync(
                existingRecord,
                existingEvents,
                request,
                cancellationToken);
        }

        var newRecord =
            new AttendanceRecord(
                Guid.NewGuid(),
                context.EmployeeId,
                context.EmploymentPeriodId,
                context.WorkScheduleAssignmentId,
                context.WorkScheduleId,
                context.WorkDate,
                context.TimeZoneId,
                context.IsWorkingDay,
                context.ExpectedStartTime,
                context.ExpectedEndTime,
                context.ExpectedBreakMinutes,
                context.ExpectationSource,
                context.ExpectationSourceId,
                context.ExpectationSourceName);

        try
        {
            AttendancePunchSequencePolicy
                .EnsureCanAppend(
                    [],
                    request.EventType,
                    request.OccurredAtUtc);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Failure(
                exception.Message);
        }

        var newEvent =
            new AttendanceEvent(
                Guid.NewGuid(),
                newRecord.Id,
                request.EmployeeId,
                request.EventType,
                request.OccurredAtUtc);

        await _persistence
            .AppendAsync(
                newRecord,
                newEvent,
                expectedLastEvent: null,
                cancellationToken);

        return Success(
            newRecord,
            newEvent);
    }

    private async Task<RecordAttendancePunchResult>
        AppendToExistingRecordAsync(
            AttendanceRecord record,
            IReadOnlyList<AttendanceEvent> existingEvents,
            RecordAttendancePunchRequest request,
            CancellationToken cancellationToken)
    {
        try
        {
            AttendancePunchSequencePolicy
                .EnsureCanAppend(
                    existingEvents,
                    request.EventType,
                    request.OccurredAtUtc);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Failure(
                exception.Message);
        }

        AttendanceEvent? expectedLastEvent =
            existingEvents.Count == 0
                ? null
                : existingEvents[
                    existingEvents.Count - 1];

        var newEvent =
            new AttendanceEvent(
                Guid.NewGuid(),
                record.Id,
                request.EmployeeId,
                request.EventType,
                request.OccurredAtUtc);

        await _persistence
            .AppendAsync(
                newRecord: null,
                newEvent,
                expectedLastEvent,
                cancellationToken);

        return Success(
            record,
            newEvent);
    }

    private static RecordAttendancePunchResult Success(
        AttendanceRecord record,
        AttendanceEvent attendanceEvent)
    {
        return new RecordAttendancePunchResult(
            IsSuccessful: true,
            AttendanceRecordId: record.Id,
            AttendanceEventId: attendanceEvent.Id,
            WorkDate: record.WorkDate);
    }

    private static RecordAttendancePunchResult Failure(
        string errorMessage)
    {
        return new RecordAttendancePunchResult(
            IsSuccessful: false,
            ErrorMessage: errorMessage);
    }
}
