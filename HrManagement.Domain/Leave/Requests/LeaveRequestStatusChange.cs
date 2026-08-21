namespace HrManagement.Domain.Leave.Requests;

public sealed class LeaveRequestStatusChange
{
    public Guid Id
    {
        get;
    }

    public Guid LeaveRequestId
    {
        get;
    }

    public LeaveRequestStatus FromStatus
    {
        get;
    }

    public LeaveRequestStatus ToStatus
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

    public LeaveRequestStatusChange(
        Guid id,
        Guid leaveRequestId,
        LeaveRequestStatus fromStatus,
        LeaveRequestStatus toStatus,
        DateTime changedAtUtc,
        string changedByUserId,
        string changedByUsername,
        string? note = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã lịch sử trạng thái không hợp lệ.",
                nameof(id));
        }

        if (leaveRequestId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã đơn nghỉ phép không hợp lệ.",
                nameof(leaveRequestId));
        }

        LeaveRequestStatusTransitionPolicy
            .EnsureCanTransition(
                fromStatus,
                toStatus);

        if (changedAtUtc == default)
        {
            throw new ArgumentException(
                "Thời điểm thay đổi trạng thái không hợp lệ.",
                nameof(changedAtUtc));
        }

        if (changedAtUtc.Kind !=
            DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Thời điểm thay đổi trạng thái phải được lưu theo UTC.",
                nameof(changedAtUtc));
        }

        if (string.IsNullOrWhiteSpace(
                changedByUserId))
        {
            throw new ArgumentException(
                "Người thực hiện thay đổi trạng thái không hợp lệ.",
                nameof(changedByUserId));
        }

        if (string.IsNullOrWhiteSpace(
                changedByUsername))
        {
            throw new ArgumentException(
                "Tên đăng nhập của người thực hiện không hợp lệ.",
                nameof(changedByUsername));
        }

        string? normalizedNote =
            string.IsNullOrWhiteSpace(
                note)
                ? null
                : note.Trim();

        if (normalizedNote?.Length >
            1000)
        {
            throw new ArgumentException(
                "Ghi chú thay đổi trạng thái không được vượt quá 1000 ký tự.",
                nameof(note));
        }

        Id =
            id;

        LeaveRequestId =
            leaveRequestId;

        FromStatus =
            fromStatus;

        ToStatus =
            toStatus;

        ChangedAtUtc =
            changedAtUtc;

        ChangedByUserId =
            changedByUserId.Trim();

        ChangedByUsername =
            changedByUsername.Trim();

        Note =
            normalizedNote;
    }
}
