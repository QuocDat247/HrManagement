namespace HrManagement.Domain.Leave.Requests;

public sealed class LeaveRequest
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

    public Guid LeaveTypeId
    {
        get;
    }

    public DateOnly StartDate
    {
        get;
    }

    public DateOnly EndDate
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

    public LeaveRequestStatus Status
    {
        get;
        private set;
    }

    public LeaveRequest(
        Guid id,
        Guid employeeId,
        Guid employmentPeriodId,
        Guid leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        string? reason,
        DateTime submittedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã đơn nghỉ phép không hợp lệ.",
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

        if (leaveTypeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã loại nghỉ phép không hợp lệ.",
                nameof(leaveTypeId));
        }

        if (startDate == default)
        {
            throw new ArgumentException(
                "Ngày bắt đầu nghỉ không hợp lệ.",
                nameof(startDate));
        }

        if (endDate == default)
        {
            throw new ArgumentException(
                "Ngày kết thúc nghỉ không hợp lệ.",
                nameof(endDate));
        }

        if (endDate < startDate)
        {
            throw new ArgumentException(
                "Ngày kết thúc nghỉ không thể trước ngày bắt đầu nghỉ.",
                nameof(endDate));
        }

        if (submittedAtUtc == default)
        {
            throw new ArgumentException(
                "Thời điểm gửi đơn không hợp lệ.",
                nameof(submittedAtUtc));
        }

        if (submittedAtUtc.Kind !=
            DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Thời điểm gửi đơn phải được lưu theo UTC.",
                nameof(submittedAtUtc));
        }

        Id =
            id;

        EmployeeId =
            employeeId;

        EmploymentPeriodId =
            employmentPeriodId;

        LeaveTypeId =
            leaveTypeId;

        StartDate =
            startDate;

        EndDate =
            endDate;

        Reason =
            string.IsNullOrWhiteSpace(
                reason)
                ? null
                : reason.Trim();

        SubmittedAtUtc =
            submittedAtUtc;

        Status =
            LeaveRequestStatus.Pending;
    }
}
