using HrManagement.Application.Attendance.Records;
using HrManagement.Domain.Attendance.Corrections;
using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Application.Attendance.Corrections;

public sealed class AttendanceCorrectionWorkspaceQueryService
    : IAttendanceCorrectionWorkspaceQueryService
{
    private readonly IAttendanceRecordRepository
        _recordRepository;

    private readonly IAttendanceEventRepository
        _eventRepository;

    private readonly IAttendanceCorrectionPersistence
        _correctionPersistence;

    private readonly IEffectiveAttendanceTimelineResolver
        _timelineResolver;

    public AttendanceCorrectionWorkspaceQueryService(
        IAttendanceRecordRepository recordRepository,
        IAttendanceEventRepository eventRepository,
        IAttendanceCorrectionPersistence correctionPersistence,
        IEffectiveAttendanceTimelineResolver timelineResolver)
    {
        _recordRepository =
            recordRepository;

        _eventRepository =
            eventRepository;

        _correctionPersistence =
            correctionPersistence;

        _timelineResolver =
            timelineResolver;
    }

    public async Task<AttendanceCorrectionWorkspaceSnapshot?>
        GetAsync(
            Guid attendanceRecordId,
            CancellationToken cancellationToken = default)
    {
        if (attendanceRecordId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã bản ghi chấm công không hợp lệ.",
                nameof(attendanceRecordId));
        }

        AttendanceRecord? record =
            await _recordRepository
                .GetByIdAsync(
                    attendanceRecordId,
                    cancellationToken);

        if (record is null)
        {
            return null;
        }

        IReadOnlyList<AttendanceEvent> rawEvents =
            await _eventRepository
                .GetByAttendanceRecordIdAsync(
                    record.Id,
                    cancellationToken);

        IReadOnlyList<AttendanceCorrection> corrections =
            await _correctionPersistence
                .GetByAttendanceRecordIdAsync(
                    record.Id,
                    cancellationToken);

        IReadOnlyList<EffectiveAttendanceEvent>
            effectiveTimeline =
                _timelineResolver.Resolve(
                    record.Id,
                    record.EmployeeId,
                    rawEvents,
                    corrections);

        TimeZoneInfo timeZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                record.TimeZoneId);

        AttendanceCorrectionWorkspaceEventItem[]
            effectiveItems =
                effectiveTimeline
                    .Select(
                        item =>
                            new AttendanceCorrectionWorkspaceEventItem(
                                item.EventId,
                                item.EventType,
                                item.OccurredAtUtc,
                                ToLocal(
                                    item.OccurredAtUtc,
                                    timeZone),
                                item.IsManual,
                                item.IsCorrected,
                                item.LastCorrectionRevision))
                    .ToArray();

        AttendanceCorrectionWorkspaceHistoryItem[]
            historyItems =
                corrections
                    .OrderByDescending(
                        correction =>
                            correction.Revision)
                    .Select(
                        correction =>
                            new AttendanceCorrectionWorkspaceHistoryItem(
                                correction.Id,
                                correction.Revision,
                                correction.Kind,
                                correction.AffectedEventId,
                                correction.BeforeEventType,
                                correction.BeforeOccurredAtUtc,
                                ToLocal(
                                    correction.BeforeOccurredAtUtc,
                                    timeZone),
                                correction.AfterEventType,
                                correction.AfterOccurredAtUtc,
                                ToLocal(
                                    correction.AfterOccurredAtUtc,
                                    timeZone),
                                correction.Reason,
                                correction.CorrectedAtUtc,
                                correction.ActorUsername))
                    .ToArray();

        return new AttendanceCorrectionWorkspaceSnapshot(
            record.Id,
            record.EmployeeId,
            record.WorkDate,
            record.TimeZoneId,
            effectiveItems,
            historyItems);
    }

    private static DateTime ToLocal(
        DateTime utc,
        TimeZoneInfo timeZone)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(
            utc,
            timeZone);
    }

    private static DateTime? ToLocal(
        DateTime? utc,
        TimeZoneInfo timeZone)
    {
        return utc.HasValue
            ? ToLocal(
                utc.Value,
                timeZone)
            : null;
    }
}
