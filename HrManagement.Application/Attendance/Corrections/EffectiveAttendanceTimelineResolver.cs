using HrManagement.Domain.Attendance.Corrections;
using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Application.Attendance.Corrections;

public sealed class EffectiveAttendanceTimelineResolver
    : IEffectiveAttendanceTimelineResolver
{
    public IReadOnlyList<EffectiveAttendanceEvent> Resolve(
        Guid attendanceRecordId,
        Guid employeeId,
        IReadOnlyCollection<AttendanceEvent> rawEvents,
        IReadOnlyCollection<AttendanceCorrection> corrections)
    {
        if (attendanceRecordId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã bản ghi chấm công không hợp lệ.",
                nameof(attendanceRecordId));
        }

        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        ArgumentNullException.ThrowIfNull(
            rawEvents);

        ArgumentNullException.ThrowIfNull(
            corrections);

        var states =
            new Dictionary<Guid, EventState>();

        var historicallyUsedEventIds =
            new HashSet<Guid>();

        foreach (AttendanceEvent rawEvent in rawEvents)
        {
            EnsureRawEventBelongsToTimeline(
                rawEvent,
                attendanceRecordId,
                employeeId);

            if (!historicallyUsedEventIds.Add(
                    rawEvent.Id))
            {
                throw new InvalidOperationException(
                    "Phát hiện trùng mã sự kiện chấm công gốc.");
            }

            states.Add(
                rawEvent.Id,
                new EventState(
                    rawEvent.EventType,
                    rawEvent.OccurredAtUtc,
                    IsManual: false,
                    IsCorrected: false,
                    LastCorrectionRevision: null));
        }

        AttendanceCorrection[] orderedCorrections =
            corrections
                .OrderBy(
                    correction =>
                        correction.Revision)
                .ToArray();

        ValidateCorrectionsBelongToTimeline(
            orderedCorrections,
            attendanceRecordId,
            employeeId);

        ValidateRevisionSequence(
            orderedCorrections);

        foreach (AttendanceCorrection correction
                 in orderedCorrections)
        {
            switch (correction.Kind)
            {
                case AttendanceCorrectionKind.AddEvent:
                    ApplyAdd(
                        correction,
                        states,
                        historicallyUsedEventIds);

                    break;

                case AttendanceCorrectionKind.ChangeEvent:
                    ApplyChange(
                        correction,
                        states);

                    break;

                case AttendanceCorrectionKind.VoidEvent:
                    ApplyVoid(
                        correction,
                        states);

                    break;

                default:
                    throw new InvalidOperationException(
                        "Loại điều chỉnh chấm công không được hỗ trợ.");
            }
        }

        return states
            .Select(
                pair =>
                    new EffectiveAttendanceEvent(
                        pair.Key,
                        attendanceRecordId,
                        employeeId,
                        pair.Value.EventType,
                        pair.Value.OccurredAtUtc,
                        pair.Value.IsManual,
                        pair.Value.IsCorrected,
                        pair.Value.LastCorrectionRevision))
            .OrderBy(
                item =>
                    item.OccurredAtUtc)
            .ThenBy(
                item =>
                    item.EventId)
            .ToArray();
    }

    private static void EnsureRawEventBelongsToTimeline(
        AttendanceEvent rawEvent,
        Guid attendanceRecordId,
        Guid employeeId)
    {
        if (rawEvent.AttendanceRecordId !=
            attendanceRecordId)
        {
            throw new InvalidOperationException(
                "Sự kiện chấm công không thuộc bản ghi đang được xử lý.");
        }

        if (rawEvent.EmployeeId !=
            employeeId)
        {
            throw new InvalidOperationException(
                "Sự kiện chấm công không thuộc nhân viên đang được xử lý.");
        }
    }

    private static void ValidateCorrectionsBelongToTimeline(
        IReadOnlyCollection<AttendanceCorrection> corrections,
        Guid attendanceRecordId,
        Guid employeeId)
    {
        foreach (AttendanceCorrection correction
                 in corrections)
        {
            if (correction.AttendanceRecordId !=
                attendanceRecordId)
            {
                throw new InvalidOperationException(
                    "Điều chỉnh chấm công không thuộc bản ghi đang được xử lý.");
            }

            if (correction.EmployeeId !=
                employeeId)
            {
                throw new InvalidOperationException(
                    "Điều chỉnh chấm công không thuộc nhân viên đang được xử lý.");
            }
        }
    }

    private static void ValidateRevisionSequence(
        IReadOnlyList<AttendanceCorrection> corrections)
    {
        for (int index = 0;
             index < corrections.Count;
             index++)
        {
            int expectedRevision =
                index + 1;

            if (corrections[index].Revision !=
                expectedRevision)
            {
                throw new InvalidOperationException(
                    "Chuỗi phiên bản điều chỉnh chấm công không liên tục.");
            }
        }
    }

    private static void ApplyAdd(
        AttendanceCorrection correction,
        IDictionary<Guid, EventState> states,
        ISet<Guid> historicallyUsedEventIds)
    {
        if (historicallyUsedEventIds.Contains(
                correction.AffectedEventId))
        {
            throw new InvalidOperationException(
                "Không thể thêm lại một mã sự kiện chấm công đã tồn tại.");
        }

        AttendanceEventType eventType =
            correction.AfterEventType
            ?? throw new InvalidOperationException(
                "Điều chỉnh thêm sự kiện thiếu loại sự kiện sau điều chỉnh.");

        DateTime occurredAtUtc =
            correction.AfterOccurredAtUtc
            ?? throw new InvalidOperationException(
                "Điều chỉnh thêm sự kiện thiếu thời điểm sau điều chỉnh.");

        historicallyUsedEventIds.Add(
            correction.AffectedEventId);

        states.Add(
            correction.AffectedEventId,
            new EventState(
                eventType,
                occurredAtUtc,
                IsManual: true,
                IsCorrected: true,
                LastCorrectionRevision:
                    correction.Revision));
    }

    private static void ApplyChange(
        AttendanceCorrection correction,
        IDictionary<Guid, EventState> states)
    {
        if (!states.TryGetValue(
                correction.AffectedEventId,
                out EventState? currentState))
        {
            throw new InvalidOperationException(
                "Không thể sửa sự kiện chấm công không còn tồn tại trong timeline.");
        }

        EnsureBeforeStateMatches(
            correction,
            currentState);

        AttendanceEventType eventType =
            correction.AfterEventType
            ?? throw new InvalidOperationException(
                "Điều chỉnh sửa sự kiện thiếu loại sự kiện sau điều chỉnh.");

        DateTime occurredAtUtc =
            correction.AfterOccurredAtUtc
            ?? throw new InvalidOperationException(
                "Điều chỉnh sửa sự kiện thiếu thời điểm sau điều chỉnh.");

        states[correction.AffectedEventId] =
            currentState with
            {
                EventType =
                    eventType,

                OccurredAtUtc =
                    occurredAtUtc,

                IsCorrected =
                    true,

                LastCorrectionRevision =
                    correction.Revision
            };
    }

    private static void ApplyVoid(
        AttendanceCorrection correction,
        IDictionary<Guid, EventState> states)
    {
        if (!states.TryGetValue(
                correction.AffectedEventId,
                out EventState? currentState))
        {
            throw new InvalidOperationException(
                "Không thể hủy sự kiện chấm công không còn tồn tại trong timeline.");
        }

        EnsureBeforeStateMatches(
            correction,
            currentState);

        states.Remove(
            correction.AffectedEventId);
    }

    private static void EnsureBeforeStateMatches(
        AttendanceCorrection correction,
        EventState currentState)
    {
        if (correction.BeforeEventType !=
                currentState.EventType
            || correction.BeforeOccurredAtUtc !=
                currentState.OccurredAtUtc)
        {
            throw new InvalidOperationException(
                "Trạng thái trước điều chỉnh không còn khớp với timeline hiện tại.");
        }
    }

    private sealed record EventState(
        AttendanceEventType EventType,
        DateTime OccurredAtUtc,
        bool IsManual,
        bool IsCorrected,
        int? LastCorrectionRevision);
}
