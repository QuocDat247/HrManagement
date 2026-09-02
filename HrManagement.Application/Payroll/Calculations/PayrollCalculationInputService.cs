using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Application.Payroll.Compensation;

namespace HrManagement.Application.Payroll.Calculations;

public sealed class PayrollCalculationInputService
    : IPayrollCalculationInputService
{
    private readonly IMonthlyTimesheetQueryService
        _timesheetQueryService;

    private readonly IEmployeeCompensationQuerySource
        _compensationQuerySource;

    private readonly IApprovedOvertimePayrollSource
        _overtimeSource;

    public PayrollCalculationInputService(
        IMonthlyTimesheetQueryService timesheetQueryService,
        IEmployeeCompensationQuerySource compensationQuerySource,
        IApprovedOvertimePayrollSource overtimeSource)
    {
        _timesheetQueryService =
            timesheetQueryService;

        _compensationQuerySource =
            compensationQuerySource;

        _overtimeSource =
            overtimeSource;
    }

    public async Task<PayrollCalculationInput> GetAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(
            year,
            month);

        DateOnly periodStart =
            new(
                year,
                month,
                1);

        DateOnly periodEnd =
            new(
                year,
                month,
                DateTime.DaysInMonth(
                    year,
                    month));

        MonthlyTimesheetReadModel timesheet =
            await _timesheetQueryService
                .GetAsync(
                    year,
                    month,
                    cancellationToken);

        if (!timesheet.IsClosed
            || !timesheet.TimesheetPeriodId.HasValue)
        {
            return new PayrollCalculationInput(
                year,
                month,
                timesheet.TimesheetPeriodId,
                timesheet.IsClosed,
                [],
                [
                    new PayrollCalculationIssue(
                        PayrollCalculationIssueCode
                            .TimesheetNotClosed,
                        null,
                        "Bảng công tháng phải được đóng trước khi chuẩn bị dữ liệu tính lương.")
                ]);
        }

        Guid[] employeeIds =
            timesheet.Items
                .Select(
                    item =>
                        item.EmployeeId)
                .Distinct()
                .ToArray();

        if (employeeIds.Length == 0)
        {
            return new PayrollCalculationInput(
                year,
                month,
                timesheet.TimesheetPeriodId,
                true,
                [],
                []);
        }

        IReadOnlyList<EmployeeCompensationSegment>
            compensationSegments =
                await _compensationQuerySource
                    .GetForPeriodAsync(
                        employeeIds,
                        periodStart,
                        periodEnd,
                        cancellationToken);

        IReadOnlyList<ApprovedOvertimePayrollItem>
            approvedOvertime =
                await _overtimeSource
                    .GetApprovedAsync(
                        employeeIds,
                        periodStart,
                        periodEnd,
                        cancellationToken);

        var issues =
            new List<PayrollCalculationIssue>();

        var employees =
            new List<PayrollEmployeeCalculationInput>();

        foreach (
            IGrouping<Guid, MonthlyTimesheetDayItem>
                employeeGroup
            in timesheet.Items
                .GroupBy(
                    item =>
                        item.EmployeeId)
                .OrderBy(
                    group =>
                        group.First().EmployeeCode)
                .ThenBy(
                    group =>
                        group.Key))
        {
            Guid employeeId =
                employeeGroup.Key;

            MonthlyTimesheetDayItem[] days =
                employeeGroup
                    .OrderBy(
                        day =>
                            day.WorkDate)
                    .ToArray();

            EmployeeCompensationSegment[] employeeCompensations =
                compensationSegments
                    .Where(
                        compensation =>
                            compensation.EmployeeId ==
                                employeeId)
                    .OrderBy(
                        compensation =>
                            compensation.EffectiveFrom)
                    .ThenBy(
                        compensation =>
                            compensation.CompensationId)
                    .ToArray();

            ApprovedOvertimePayrollItem[] employeeOvertime =
                approvedOvertime
                    .Where(
                        overtime =>
                            overtime.EmployeeId ==
                                employeeId)
                    .OrderBy(
                        overtime =>
                            overtime.WorkDate)
                    .ThenBy(
                        overtime =>
                            overtime.OvertimeRequestId)
                    .ToArray();

            ValidateCompensationCoverage(
                employeeId,
                days,
                employeeCompensations,
                issues);

            ValidateCurrencyConsistency(
                employeeId,
                employeeCompensations,
                issues);

            ValidateOvertimeCoverage(
                employeeId,
                days,
                employeeOvertime,
                issues);

            MonthlyTimesheetDayItem firstDay =
                days[0];

            employees.Add(
                new PayrollEmployeeCalculationInput(
                    employeeId,
                    firstDay.EmployeeCode,
                    firstDay.EmployeeFullName,
                    days,
                    employeeCompensations,
                    employeeOvertime));
        }

        return new PayrollCalculationInput(
            year,
            month,
            timesheet.TimesheetPeriodId,
            true,
            employees,
            issues);
    }

    private static void ValidateCompensationCoverage(
        Guid employeeId,
        IReadOnlyList<MonthlyTimesheetDayItem> days,
        IReadOnlyList<EmployeeCompensationSegment> compensations,
        ICollection<PayrollCalculationIssue> issues)
    {
        foreach (MonthlyTimesheetDayItem day in days)
        {
            int matchingCount =
                compensations.Count(
                    compensation =>
                        compensation.EffectiveFrom <=
                            day.WorkDate
                        && (
                            !compensation.EffectiveTo.HasValue
                            || compensation.EffectiveTo.Value >=
                                day.WorkDate
                        ));

            if (matchingCount == 0)
            {
                issues.Add(
                    new PayrollCalculationIssue(
                        PayrollCalculationIssueCode
                            .MissingCompensation,
                        employeeId,
                        $"Nhân viên chưa có cấu hình lương bao phủ ngày {day.WorkDate:dd/MM/yyyy}."));

                return;
            }

            if (matchingCount > 1)
            {
                issues.Add(
                    new PayrollCalculationIssue(
                        PayrollCalculationIssueCode
                            .OverlappingCompensation,
                        employeeId,
                        $"Có nhiều cấu hình lương cùng bao phủ ngày {day.WorkDate:dd/MM/yyyy}."));

                return;
            }
        }
    }

    private static void ValidateCurrencyConsistency(
        Guid employeeId,
        IReadOnlyList<EmployeeCompensationSegment> compensations,
        ICollection<PayrollCalculationIssue> issues)
    {
        string[] currencyCodes =
            compensations
                .Select(
                    compensation =>
                        compensation.CurrencyCode)
                .Distinct(
                    StringComparer.Ordinal)
                .ToArray();

        if (currencyCodes.Length <= 1)
        {
            return;
        }

        issues.Add(
            new PayrollCalculationIssue(
                PayrollCalculationIssueCode
                    .MixedCompensationCurrency,
                employeeId,
                "Một kỳ lương không thể sử dụng nhiều loại tiền tệ cho cùng nhân viên."));
    }

    private static void ValidateOvertimeCoverage(
        Guid employeeId,
        IReadOnlyList<MonthlyTimesheetDayItem> days,
        IReadOnlyList<ApprovedOvertimePayrollItem> approvedOvertime,
        ICollection<PayrollCalculationIssue> issues)
    {
        HashSet<DateOnly> timesheetDates =
            days
                .Select(
                    day =>
                        day.WorkDate)
                .ToHashSet();

        ApprovedOvertimePayrollItem? missingDay =
            approvedOvertime
                .FirstOrDefault(
                    overtime =>
                        !timesheetDates.Contains(
                            overtime.WorkDate));

        if (missingDay is null)
        {
            return;
        }

        issues.Add(
            new PayrollCalculationIssue(
                PayrollCalculationIssueCode
                    .OvertimeWithoutTimesheetDay,
                employeeId,
                $"Yêu cầu tăng ca đã duyệt ngày {missingDay.WorkDate:dd/MM/yyyy} không có snapshot bảng công tương ứng."));
    }

    private static void ValidatePeriod(
        int year,
        int month)
    {
        if (year < 2000
            || year > 9999)
        {
            throw new ArgumentOutOfRangeException(
                nameof(year),
                "Năm kỳ lương không hợp lệ.");
        }

        if (month < 1
            || month > 12)
        {
            throw new ArgumentOutOfRangeException(
                nameof(month),
                "Tháng kỳ lương phải từ 1 đến 12.");
        }
    }
}
