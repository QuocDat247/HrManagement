using HrManagement.Domain.Employees;
using HrManagement.Domain.Leave.Types;

namespace HrManagement.Domain.Leave.Policies;

public static class LeaveRequestEligibilityPolicy
{
    public static void EnsureCanRequest(
        Guid employeeId,
        EmploymentPeriod employmentPeriod,
        LeaveType leaveType,
        DateOnly startDate,
        DateOnly endDate)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        ArgumentNullException.ThrowIfNull(
            employmentPeriod);

        ArgumentNullException.ThrowIfNull(
            leaveType);

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

        if (employmentPeriod.EmployeeId !=
            employeeId)
        {
            throw new InvalidOperationException(
                "Giai đoạn làm việc không thuộc nhân viên đang tạo đơn nghỉ phép.");
        }

        if (startDate <
            employmentPeriod.StartDate)
        {
            throw new InvalidOperationException(
                "Ngày bắt đầu nghỉ nằm trước giai đoạn làm việc.");
        }

        if (employmentPeriod.EndDate.HasValue
            && endDate >
                employmentPeriod.EndDate.Value)
        {
            throw new InvalidOperationException(
                "Ngày kết thúc nghỉ vượt quá giai đoạn làm việc.");
        }

        if (!leaveType.IsActive)
        {
            throw new InvalidOperationException(
                "Loại nghỉ phép đã ngừng sử dụng.");
        }
    }
}
