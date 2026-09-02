using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Application.Payroll.Compensation;

namespace HrManagement.Application.Payroll.Calculations;

public sealed record PayrollEmployeeCalculationInput(
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeFullName,
    IReadOnlyList<MonthlyTimesheetDayItem> TimesheetDays,
    IReadOnlyList<EmployeeCompensationSegment> CompensationSegments,
    IReadOnlyList<ApprovedOvertimePayrollItem> ApprovedOvertime);
