using HrManagement.Domain.Leave.Requests;

namespace HrManagement.Domain.Leave.Policies;

public static class LeaveRequestOverlapPolicy
{
    public static void EnsureNoOverlap(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<LeaveRequest> existingRequests)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
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
                "Ngày kết thúc nghỉ không thể trước ngày bắt đầu.",
                nameof(endDate));
        }

        ArgumentNullException.ThrowIfNull(
            existingRequests);

        foreach (LeaveRequest existingRequest
                 in existingRequests)
        {
            if (existingRequest.EmployeeId !=
                employeeId)
            {
                continue;
            }

            if (existingRequest.Status is
                LeaveRequestStatus.Rejected
                or LeaveRequestStatus.Cancelled)
            {
                continue;
            }

            bool overlaps =
                startDate <=
                    existingRequest.EndDate
                && existingRequest.StartDate <=
                    endDate;

            if (overlaps)
            {
                throw new InvalidOperationException(
                    "Khoảng nghỉ phép bị trùng với một đơn nghỉ phép đang có hiệu lực.");
            }
        }
    }
}
