namespace HrManagement.Application.Payroll.Calculations;

public sealed class PayrollPolicyProfile
{
    public int StandardMonthlyWorkingMinutes
    {
        get;
    }

    public decimal NonWorkingDayOvertimeMultiplier
    {
        get;
    }

    public PayrollPolicyProfile(
        int standardMonthlyWorkingMinutes,
        decimal nonWorkingDayOvertimeMultiplier)
    {
        if (standardMonthlyWorkingMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(standardMonthlyWorkingMinutes),
                "Số phút làm việc chuẩn tháng phải lớn hơn 0.");
        }

        if (standardMonthlyWorkingMinutes >
            31 * 24 * 60)
        {
            throw new ArgumentOutOfRangeException(
                nameof(standardMonthlyWorkingMinutes),
                "Số phút làm việc chuẩn tháng vượt quá giới hạn hợp lệ.");
        }

        if (nonWorkingDayOvertimeMultiplier <= 0m
            || nonWorkingDayOvertimeMultiplier > 10m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nonWorkingDayOvertimeMultiplier),
                "Hệ số tăng ca ngoài ngày làm việc không hợp lệ.");
        }

        StandardMonthlyWorkingMinutes =
            standardMonthlyWorkingMinutes;

        NonWorkingDayOvertimeMultiplier =
            nonWorkingDayOvertimeMultiplier;
    }
}
