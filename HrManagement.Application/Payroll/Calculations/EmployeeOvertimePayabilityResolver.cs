using HrManagement.Application.Attendance.Timesheets;

namespace HrManagement.Application.Payroll.Calculations;

public sealed class EmployeeOvertimePayabilityResolver
    : IEmployeeOvertimePayabilityResolver
{
    private readonly IOvertimePayabilityPolicy
        _policy;

    public EmployeeOvertimePayabilityResolver(
        IOvertimePayabilityPolicy policy)
    {
        _policy =
            policy;
    }

    public IReadOnlyList<OvertimePayabilityResolution> Resolve(
        PayrollEmployeeCalculationInput employeeInput)
    {
        ArgumentNullException.ThrowIfNull(
            employeeInput);

        if (employeeInput.ApprovedOvertime.Count == 0)
        {
            return [];
        }

        Dictionary<DateOnly, MonthlyTimesheetDayItem>
            daysByDate =
                employeeInput
                    .TimesheetDays
                    .ToDictionary(
                        day =>
                            day.WorkDate);

        var results =
            new List<OvertimePayabilityResolution>();

        foreach (
            ApprovedOvertimePayrollItem overtime
            in employeeInput
                .ApprovedOvertime
                .OrderBy(
                    item =>
                        item.WorkDate)
                .ThenBy(
                    item =>
                        item.OvertimeRequestId))
        {
            if (!daysByDate.TryGetValue(
                    overtime.WorkDate,
                    out MonthlyTimesheetDayItem? day))
            {
                throw new InvalidOperationException(
                    $"Không tìm thấy snapshot bảng công cho yêu cầu tăng ca ngày {overtime.WorkDate:dd/MM/yyyy}.");
            }

            results.Add(
                _policy.Resolve(
                    day,
                    overtime));
        }

        return results;
    }
}
