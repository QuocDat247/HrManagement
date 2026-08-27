namespace HrManagement.Domain.Overtime.Requests;

public sealed class OvertimeRequest
{
    public Guid Id
    {
        get;
    }

    public Guid EmployeeId
    {
        get;
    }

    public Guid EmploymentPeriodId
    {
        get;
    }

    public DateOnly WorkDate
    {
        get;
    }

    public int RequestedMinutes
    {
        get;
    }

    public string? Reason
    {
        get;
    }

    public DateTime SubmittedAtUtc
    {
        get;
    }

    public OvertimeRequestStatus Status
    {
        get;
        private set;
    }

    public int? ApprovedMinutes
    {
        get;
        private set;
    }

    public OvertimeRequest(
        Guid id,
        Guid employeeId,
        Guid employmentPeriodId,
        DateOnly workDate,
        int requestedMinutes,
        string? reason,
        DateTime submittedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã yêu cầu tăng ca không hợp lệ.",
                nameof(id));
        }

        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        if (employmentPeriodId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã giai đoạn làm việc không hợp lệ.",
                nameof(employmentPeriodId));
        }

        if (workDate == default)
        {
            throw new ArgumentException(
                "Ngày tăng ca không hợp lệ.",
                nameof(workDate));
        }

        if (requestedMinutes is <= 0 or > 1440)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedMinutes),
                "Số phút tăng ca yêu cầu phải từ 1 đến 1440 phút.");
        }

        string? normalizedReason =
            string.IsNullOrWhiteSpace(reason)
                ? null
                : reason.Trim();

        if (normalizedReason?.Length > 500)
        {
            throw new ArgumentException(
                "Lý do tăng ca không được vượt quá 500 ký tự.",
                nameof(reason));
        }

        if (submittedAtUtc == default
            || submittedAtUtc.Kind !=
                DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Thời điểm gửi yêu cầu tăng ca phải sử dụng UTC.",
                nameof(submittedAtUtc));
        }

        Id =
            id;

        EmployeeId =
            employeeId;

        EmploymentPeriodId =
            employmentPeriodId;

        WorkDate =
            workDate;

        RequestedMinutes =
            requestedMinutes;

        Reason =
            normalizedReason;

        SubmittedAtUtc =
            submittedAtUtc;

        Status =
            OvertimeRequestStatus.Pending;
    }

    public OvertimeRequestStatusChange TransitionTo(
        Guid statusChangeId,
        OvertimeRequestStatus targetStatus,
        DateTime changedAtUtc,
        string changedByUserId,
        string changedByUsername,
        int? approvedMinutes = null,
        string? note = null)
    {
        OvertimeRequestStatus previousStatus =
            Status;

        OvertimeRequestStatusTransitionPolicy
            .EnsureCanTransition(
                previousStatus,
                targetStatus);

        if (targetStatus ==
            OvertimeRequestStatus.Approved)
        {
            if (!approvedMinutes.HasValue
                || approvedMinutes.Value <= 0
                || approvedMinutes.Value >
                    RequestedMinutes)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(approvedMinutes),
                    "Số phút tăng ca được duyệt phải từ 1 đến số phút đã yêu cầu.");
            }
        }
        else if (approvedMinutes.HasValue)
        {
            throw new ArgumentException(
                "Chỉ trạng thái đã duyệt mới được có số phút tăng ca được duyệt.",
                nameof(approvedMinutes));
        }

        var statusChange =
            new OvertimeRequestStatusChange(
                statusChangeId,
                Id,
                previousStatus,
                targetStatus,
                approvedMinutes,
                changedAtUtc,
                changedByUserId,
                changedByUsername,
                note);

        Status =
            targetStatus;

        ApprovedMinutes =
            targetStatus ==
                OvertimeRequestStatus.Approved
                ? approvedMinutes
                : null;

        return statusChange;
    }
}
