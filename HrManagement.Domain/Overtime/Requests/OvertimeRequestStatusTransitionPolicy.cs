namespace HrManagement.Domain.Overtime.Requests;

public static class OvertimeRequestStatusTransitionPolicy
{
    public static void EnsureCanTransition(
        OvertimeRequestStatus currentStatus,
        OvertimeRequestStatus targetStatus)
    {
        if (!Enum.IsDefined(currentStatus))
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentStatus));
        }

        if (!Enum.IsDefined(targetStatus))
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetStatus));
        }

        if (currentStatus == targetStatus)
        {
            throw new InvalidOperationException(
                "Trạng thái tăng ca mới phải khác trạng thái hiện tại.");
        }

        bool isAllowed =
            currentStatus switch
            {
                OvertimeRequestStatus.Pending =>
                    targetStatus is
                        OvertimeRequestStatus.Approved
                        or OvertimeRequestStatus.Rejected
                        or OvertimeRequestStatus.Cancelled,

                OvertimeRequestStatus.Approved =>
                    targetStatus ==
                    OvertimeRequestStatus.Cancelled,

                _ =>
                    false
            };

        if (!isAllowed)
        {
            throw new InvalidOperationException(
                $"Không thể chuyển trạng thái tăng ca từ {currentStatus} sang {targetStatus}.");
        }
    }
}
