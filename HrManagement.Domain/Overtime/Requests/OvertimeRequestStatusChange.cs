namespace HrManagement.Domain.Overtime.Requests;

public sealed class OvertimeRequestStatusChange
{
    public Guid Id
    {
        get;
    }

    public Guid OvertimeRequestId
    {
        get;
    }

    public OvertimeRequestStatus PreviousStatus
    {
        get;
    }

    public OvertimeRequestStatus NewStatus
    {
        get;
    }

    public int? ApprovedMinutes
    {
        get;
    }

    public DateTime ChangedAtUtc
    {
        get;
    }

    public string ChangedByUserId
    {
        get;
    }

    public string ChangedByUsername
    {
        get;
    }

    public string? Note
    {
        get;
    }

    public OvertimeRequestStatusChange(
        Guid id,
        Guid overtimeRequestId,
        OvertimeRequestStatus previousStatus,
        OvertimeRequestStatus newStatus,
        int? approvedMinutes,
        DateTime changedAtUtc,
        string changedByUserId,
        string changedByUsername,
        string? note = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã lịch sử trạng thái tăng ca không hợp lệ.",
                nameof(id));
        }

        if (overtimeRequestId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã yêu cầu tăng ca không hợp lệ.",
                nameof(overtimeRequestId));
        }

        OvertimeRequestStatusTransitionPolicy
            .EnsureCanTransition(
                previousStatus,
                newStatus);

        if (newStatus ==
            OvertimeRequestStatus.Approved)
        {
            if (!approvedMinutes.HasValue
                || approvedMinutes.Value <= 0)
            {
                throw new ArgumentException(
                    "Số phút tăng ca được duyệt phải lớn hơn 0.",
                    nameof(approvedMinutes));
            }
        }
        else if (approvedMinutes.HasValue)
        {
            throw new ArgumentException(
                "Chỉ trạng thái đã duyệt mới được có số phút tăng ca được duyệt.",
                nameof(approvedMinutes));
        }

        if (changedAtUtc == default
            || changedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Thời điểm thay đổi trạng thái phải sử dụng UTC.",
                nameof(changedAtUtc));
        }

        if (string.IsNullOrWhiteSpace(
                changedByUserId))
        {
            throw new ArgumentException(
                "Actor user id là bắt buộc.",
                nameof(changedByUserId));
        }

        string normalizedUserId =
            changedByUserId.Trim();

        if (normalizedUserId.Length > 100)
        {
            throw new ArgumentException(
                "Actor user id không được vượt quá 100 ký tự.",
                nameof(changedByUserId));
        }

        if (string.IsNullOrWhiteSpace(
                changedByUsername))
        {
            throw new ArgumentException(
                "Actor username là bắt buộc.",
                nameof(changedByUsername));
        }

        string normalizedUsername =
            changedByUsername.Trim();

        if (normalizedUsername.Length > 150)
        {
            throw new ArgumentException(
                "Actor username không được vượt quá 150 ký tự.",
                nameof(changedByUsername));
        }

        string? normalizedNote =
            string.IsNullOrWhiteSpace(note)
                ? null
                : note.Trim();

        if (normalizedNote?.Length > 500)
        {
            throw new ArgumentException(
                "Ghi chú thay đổi trạng thái không được vượt quá 500 ký tự.",
                nameof(note));
        }

        Id =
            id;

        OvertimeRequestId =
            overtimeRequestId;

        PreviousStatus =
            previousStatus;

        NewStatus =
            newStatus;

        ApprovedMinutes =
            approvedMinutes;

        ChangedAtUtc =
            changedAtUtc;

        ChangedByUserId =
            normalizedUserId;

        ChangedByUsername =
            normalizedUsername;

        Note =
            normalizedNote;
    }
}
