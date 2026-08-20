namespace HrManagement.Domain.Attendance.Records;

public static class AttendancePunchSequencePolicy
{
    public static void EnsureCanAppend(
        IReadOnlyList<AttendanceEvent> existingEvents,
        AttendanceEventType nextEventType,
        DateTime occurredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(
            existingEvents);

        if (!Enum.IsDefined(
                nextEventType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextEventType),
                "Loại sự kiện chấm công không hợp lệ.");
        }

        if (occurredAtUtc == default)
        {
            throw new ArgumentException(
                "Thời điểm chấm công không hợp lệ.",
                nameof(occurredAtUtc));
        }

        if (occurredAtUtc.Kind !=
            DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Thời điểm chấm công phải được lưu theo UTC.",
                nameof(occurredAtUtc));
        }

        ValidateExistingTimeline(
            existingEvents);

        if (existingEvents.Count == 0)
        {
            if (nextEventType !=
                AttendanceEventType.ClockIn)
            {
                throw new InvalidOperationException(
                    "Sự kiện chấm công đầu tiên phải là ClockIn.");
            }

            return;
        }

        AttendanceEvent lastEvent =
            existingEvents[
                existingEvents.Count - 1];

        if (occurredAtUtc <=
            lastEvent.OccurredAtUtc)
        {
            throw new InvalidOperationException(
                "Thời điểm chấm công mới phải sau sự kiện gần nhất.");
        }

        AttendanceEventType expectedType =
            lastEvent.EventType ==
                AttendanceEventType.ClockIn
                ? AttendanceEventType.ClockOut
                : AttendanceEventType.ClockIn;

        if (nextEventType !=
            expectedType)
        {
            throw new InvalidOperationException(
                expectedType ==
                    AttendanceEventType.ClockIn
                    ? "Sự kiện tiếp theo phải là ClockIn."
                    : "Sự kiện tiếp theo phải là ClockOut.");
        }
    }

    private static void ValidateExistingTimeline(
        IReadOnlyList<AttendanceEvent> events)
    {
        if (events.Count == 0)
        {
            return;
        }

        AttendanceEvent firstEvent =
            events[0];

        if (firstEvent.EventType !=
            AttendanceEventType.ClockIn)
        {
            throw new InvalidOperationException(
                "Lịch sử chấm công phải bắt đầu bằng ClockIn.");
        }

        Guid attendanceRecordId =
            firstEvent.AttendanceRecordId;

        Guid employeeId =
            firstEvent.EmployeeId;

        for (int index = 1;
             index < events.Count;
             index++)
        {
            AttendanceEvent previous =
                events[index - 1];

            AttendanceEvent current =
                events[index];

            if (current.AttendanceRecordId !=
                    attendanceRecordId
                || current.EmployeeId !=
                    employeeId)
            {
                throw new InvalidOperationException(
                    "Lịch sử chấm công chứa sự kiện không thuộc cùng bản ghi và nhân viên.");
            }

            if (current.OccurredAtUtc <=
                previous.OccurredAtUtc)
            {
                throw new InvalidOperationException(
                    "Lịch sử chấm công không được có thời điểm trùng hoặc đảo thứ tự.");
            }

            AttendanceEventType expectedType =
                previous.EventType ==
                    AttendanceEventType.ClockIn
                    ? AttendanceEventType.ClockOut
                    : AttendanceEventType.ClockIn;

            if (current.EventType !=
                expectedType)
            {
                throw new InvalidOperationException(
                    "Lịch sử chấm công không tuân theo thứ tự ClockIn và ClockOut.");
            }
        }
    }
}
