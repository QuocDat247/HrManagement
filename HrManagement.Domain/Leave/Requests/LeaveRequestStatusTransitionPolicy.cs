namespace HrManagement.Domain.Leave.Requests;

public static class LeaveRequestStatusTransitionPolicy
{
    public static void EnsureCanTransition(
        LeaveRequestStatus fromStatus,
        LeaveRequestStatus toStatus)
    {
        if (!Enum.IsDefined(
                fromStatus))
        {
            throw new ArgumentOutOfRangeException(
                nameof(fromStatus),
                "Trạng thái hiện tại của đơn nghỉ phép không hợp lệ.");
        }

        if (!Enum.IsDefined(
                toStatus))
        {
            throw new ArgumentOutOfRangeException(
                nameof(toStatus),
                "Trạng thái mới của đơn nghỉ phép không hợp lệ.");
        }

        if (fromStatus ==
            toStatus)
        {
            throw new InvalidOperationException(
                "Trạng thái mới phải khác trạng thái hiện tại.");
        }

        bool isAllowed =
            fromStatus switch
            {
                LeaveRequestStatus.Pending =>
                    toStatus is
                        LeaveRequestStatus.Approved
                        or LeaveRequestStatus.Rejected
                        or LeaveRequestStatus.Cancelled,

                LeaveRequestStatus.Approved =>
                    toStatus ==
                    LeaveRequestStatus.Cancelled,

                LeaveRequestStatus.Rejected =>
                    false,

                LeaveRequestStatus.Cancelled =>
                    false,

                _ =>
                    false
            };

        if (!isAllowed)
        {
            throw new InvalidOperationException(
                $"Không thể chuyển trạng thái đơn nghỉ phép từ {fromStatus} sang {toStatus}.");
        }
    }
}
