using HrManagement.Application.Attendance.Timesheets;

namespace HrManagement.Application.Payroll.Calculations;

public interface IOvertimePayabilityPolicy
{
    OvertimePayabilityResolution Resolve(
        MonthlyTimesheetDayItem timesheetDay,
        ApprovedOvertimePayrollItem approvedOvertime);
}
