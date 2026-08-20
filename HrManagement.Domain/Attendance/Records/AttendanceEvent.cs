namespace HrManagement.Domain.Attendance.Records;

public sealed class AttendanceEvent
{
    public Guid Id
    {
        get;
    }

    public Guid AttendanceRecordId
    {
        get;
    }

    public Guid EmployeeId
    {
        get;
    }

    public AttendanceEventType EventType
    {
        get;
    }

    public DateTime OccurredAtUtc
    {
        get;
    }

    public AttendanceEvent(
        Guid id,
        Guid attendanceRecordId,
        Guid employeeId,
        AttendanceEventType eventType,
        DateTime occurredAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã sự kiện chấm công không hợp lệ.",
                nameof(id));
        }

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

        if (!Enum.IsDefined(
                eventType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(eventType),
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

        Id =
            id;

        AttendanceRecordId =
            attendanceRecordId;

        EmployeeId =
            employeeId;

        EventType =
            eventType;

        OccurredAtUtc =
            occurredAtUtc;
    }
}
