using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Domain.Attendance.Corrections;

public sealed class AttendanceCorrection
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

    public Guid AffectedEventId
    {
        get;
    }

    public int Revision
    {
        get;
    }

    public AttendanceCorrectionKind Kind
    {
        get;
    }

    public AttendanceEventType? BeforeEventType
    {
        get;
    }

    public DateTime? BeforeOccurredAtUtc
    {
        get;
    }

    public AttendanceEventType? AfterEventType
    {
        get;
    }

    public DateTime? AfterOccurredAtUtc
    {
        get;
    }

    public string Reason
    {
        get;
    }

    public DateTime CorrectedAtUtc
    {
        get;
    }

    public string ActorUserId
    {
        get;
    }

    public string ActorUsername
    {
        get;
    }

    public bool HasBeforeState =>
        BeforeEventType.HasValue;

    public bool HasAfterState =>
        AfterEventType.HasValue;

    public AttendanceCorrection(
        Guid id,
        Guid attendanceRecordId,
        Guid employeeId,
        Guid affectedEventId,
        int revision,
        AttendanceCorrectionKind kind,
        AttendanceEventType? beforeEventType,
        DateTime? beforeOccurredAtUtc,
        AttendanceEventType? afterEventType,
        DateTime? afterOccurredAtUtc,
        string reason,
        DateTime correctedAtUtc,
        string actorUserId,
        string actorUsername)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã điều chỉnh chấm công không hợp lệ.",
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

        if (affectedEventId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã sự kiện bị tác động không hợp lệ.",
                nameof(affectedEventId));
        }

        if (revision <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(revision),
                "Phiên bản điều chỉnh phải lớn hơn 0.");
        }

        if (!Enum.IsDefined(
                kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                "Loại điều chỉnh chấm công không hợp lệ.");
        }

        ValidateEventState(
            beforeEventType,
            beforeOccurredAtUtc,
            nameof(beforeEventType),
            nameof(beforeOccurredAtUtc));

        ValidateEventState(
            afterEventType,
            afterOccurredAtUtc,
            nameof(afterEventType),
            nameof(afterOccurredAtUtc));

        ValidateCorrectionTransition(
            kind,
            beforeEventType,
            beforeOccurredAtUtc,
            afterEventType,
            afterOccurredAtUtc);

        if (string.IsNullOrWhiteSpace(
                reason))
        {
            throw new ArgumentException(
                "Lý do điều chỉnh chấm công là bắt buộc.",
                nameof(reason));
        }

        string normalizedReason =
            reason.Trim();

        if (normalizedReason.Length > 500)
        {
            throw new ArgumentException(
                "Lý do điều chỉnh không được vượt quá 500 ký tự.",
                nameof(reason));
        }

        if (correctedAtUtc == default
            || correctedAtUtc.Kind !=
                DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Thời điểm điều chỉnh phải sử dụng UTC.",
                nameof(correctedAtUtc));
        }

        if (string.IsNullOrWhiteSpace(
                actorUserId))
        {
            throw new ArgumentException(
                "Actor user id là bắt buộc.",
                nameof(actorUserId));
        }

        string normalizedActorUserId =
            actorUserId.Trim();

        if (normalizedActorUserId.Length > 100)
        {
            throw new ArgumentException(
                "Actor user id không được vượt quá 100 ký tự.",
                nameof(actorUserId));
        }

        if (string.IsNullOrWhiteSpace(
                actorUsername))
        {
            throw new ArgumentException(
                "Actor username là bắt buộc.",
                nameof(actorUsername));
        }

        string normalizedActorUsername =
            actorUsername.Trim();

        if (normalizedActorUsername.Length > 150)
        {
            throw new ArgumentException(
                "Actor username không được vượt quá 150 ký tự.",
                nameof(actorUsername));
        }

        Id =
            id;

        AttendanceRecordId =
            attendanceRecordId;

        EmployeeId =
            employeeId;

        AffectedEventId =
            affectedEventId;

        Revision =
            revision;

        Kind =
            kind;

        BeforeEventType =
            beforeEventType;

        BeforeOccurredAtUtc =
            beforeOccurredAtUtc;

        AfterEventType =
            afterEventType;

        AfterOccurredAtUtc =
            afterOccurredAtUtc;

        Reason =
            normalizedReason;

        CorrectedAtUtc =
            correctedAtUtc;

        ActorUserId =
            normalizedActorUserId;

        ActorUsername =
            normalizedActorUsername;
    }

    private static void ValidateEventState(
        AttendanceEventType? eventType,
        DateTime? occurredAtUtc,
        string eventTypeParameterName,
        string occurredAtParameterName)
    {
        if (eventType.HasValue !=
            occurredAtUtc.HasValue)
        {
            throw new ArgumentException(
                "Loại sự kiện và thời điểm sự kiện phải cùng tồn tại hoặc cùng để trống.");
        }

        if (!eventType.HasValue)
        {
            return;
        }

        if (!Enum.IsDefined(
                eventType.Value))
        {
            throw new ArgumentOutOfRangeException(
                eventTypeParameterName,
                "Loại sự kiện chấm công không hợp lệ.");
        }

        if (occurredAtUtc!.Value == default
            || occurredAtUtc.Value.Kind !=
                DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Thời điểm sự kiện chấm công phải sử dụng UTC.",
                occurredAtParameterName);
        }
    }

    private static void ValidateCorrectionTransition(
        AttendanceCorrectionKind kind,
        AttendanceEventType? beforeEventType,
        DateTime? beforeOccurredAtUtc,
        AttendanceEventType? afterEventType,
        DateTime? afterOccurredAtUtc)
    {
        bool hasBefore =
            beforeEventType.HasValue;

        bool hasAfter =
            afterEventType.HasValue;

        switch (kind)
        {
            case AttendanceCorrectionKind.AddEvent:
                if (hasBefore)
                {
                    throw new ArgumentException(
                        "Điều chỉnh thêm sự kiện không được có trạng thái trước.");
                }

                if (!hasAfter)
                {
                    throw new ArgumentException(
                        "Điều chỉnh thêm sự kiện phải có trạng thái sau.");
                }

                break;

            case AttendanceCorrectionKind.ChangeEvent:
                if (!hasBefore)
                {
                    throw new ArgumentException(
                        "Điều chỉnh sửa sự kiện phải có trạng thái trước.");
                }

                if (!hasAfter)
                {
                    throw new ArgumentException(
                        "Điều chỉnh sửa sự kiện phải có trạng thái sau.");
                }

                if (beforeEventType ==
                        afterEventType
                    && beforeOccurredAtUtc ==
                        afterOccurredAtUtc)
                {
                    throw new ArgumentException(
                        "Điều chỉnh sửa sự kiện phải làm thay đổi dữ liệu.");
                }

                break;

            case AttendanceCorrectionKind.VoidEvent:
                if (!hasBefore)
                {
                    throw new ArgumentException(
                        "Điều chỉnh hủy sự kiện phải có trạng thái trước.");
                }

                if (hasAfter)
                {
                    throw new ArgumentException(
                        "Điều chỉnh hủy sự kiện không được có trạng thái sau.");
                }

                break;
        }
    }
}
