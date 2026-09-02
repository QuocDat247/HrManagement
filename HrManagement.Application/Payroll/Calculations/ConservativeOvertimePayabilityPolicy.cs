using HrManagement.Application.Attendance.Timesheets;

namespace HrManagement.Application.Payroll.Calculations;

public sealed class ConservativeOvertimePayabilityPolicy
    : IOvertimePayabilityPolicy
{
    public OvertimePayabilityResolution Resolve(
        MonthlyTimesheetDayItem timesheetDay,
        ApprovedOvertimePayrollItem approvedOvertime)
    {
        ArgumentNullException.ThrowIfNull(
            timesheetDay);

        ArgumentNullException.ThrowIfNull(
            approvedOvertime);

        ValidateInput(
            timesheetDay,
            approvedOvertime);

        if (timesheetDay.IsWorkingDay
            || timesheetDay.ExpectedPlannedMinutes > 0)
        {
            return new OvertimePayabilityResolution(
                approvedOvertime.OvertimeRequestId,
                approvedOvertime.EmployeeId,
                approvedOvertime.WorkDate,
                approvedOvertime.ApprovedMinutes,
                PayableMinutes:
                    null,
                Status:
                    OvertimePayabilityStatus.RequiresReview,
                Reason:
                    "Dữ liệu bảng công hiện tại chưa đủ để tách chính xác thời gian tăng ca khỏi giờ làm kế hoạch trong ngày làm việc.");
        }

        if (timesheetDay.WorkedMinutes <= 0)
        {
            return new OvertimePayabilityResolution(
                approvedOvertime.OvertimeRequestId,
                approvedOvertime.EmployeeId,
                approvedOvertime.WorkDate,
                approvedOvertime.ApprovedMinutes,
                PayableMinutes:
                    0,
                Status:
                    OvertimePayabilityStatus.NotPayable,
                Reason:
                    "Không có thời gian làm việc thực tế trong ngày tăng ca đã duyệt.");
        }

        int payableMinutes =
            Math.Min(
                approvedOvertime.ApprovedMinutes,
                timesheetDay.WorkedMinutes);

        return new OvertimePayabilityResolution(
            approvedOvertime.OvertimeRequestId,
            approvedOvertime.EmployeeId,
            approvedOvertime.WorkDate,
            approvedOvertime.ApprovedMinutes,
            PayableMinutes:
                payableMinutes,
            Status:
                OvertimePayabilityStatus.Payable,
            Reason:
                payableMinutes ==
                    approvedOvertime.ApprovedMinutes
                    ? "Toàn bộ số phút tăng ca được duyệt có đủ bằng chứng từ thời gian làm việc thực tế ngoài giờ kế hoạch."
                    : "Số phút được trả bị giới hạn bởi thời gian làm việc thực tế được ghi nhận.");
    }

    private static void ValidateInput(
        MonthlyTimesheetDayItem timesheetDay,
        ApprovedOvertimePayrollItem approvedOvertime)
    {
        if (approvedOvertime.OvertimeRequestId ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Mã yêu cầu tăng ca không hợp lệ.",
                nameof(approvedOvertime));
        }

        if (approvedOvertime.EmployeeId ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên tăng ca không hợp lệ.",
                nameof(approvedOvertime));
        }

        if (approvedOvertime.ApprovedMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(approvedOvertime),
                "Số phút tăng ca được duyệt phải lớn hơn 0.");
        }

        if (timesheetDay.EmployeeId !=
            approvedOvertime.EmployeeId)
        {
            throw new ArgumentException(
                "Yêu cầu tăng ca và bảng công không thuộc cùng nhân viên.");
        }

        if (timesheetDay.WorkDate !=
            approvedOvertime.WorkDate)
        {
            throw new ArgumentException(
                "Yêu cầu tăng ca và bảng công không cùng ngày.");
        }

        if (timesheetDay.WorkedMinutes < 0
            || timesheetDay.ExpectedPlannedMinutes < 0)
        {
            throw new ArgumentException(
                "Dữ liệu phút làm việc của bảng công không hợp lệ.");
        }
    }
}
