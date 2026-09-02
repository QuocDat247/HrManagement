using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Application.Payroll.Calculations;
using HrManagement.Domain.Attendance.Calculations;

namespace HrManagement.Tests.Payroll;

public sealed class EmployeeOvertimePayabilityResolverTests
{
    [Fact]
    public void Resolve_WhenEmployeeHasMultipleRequests_ReturnsOrderedResolutions()
    {
        Guid employeeId =
            Guid.NewGuid();

        var resolver =
            new EmployeeOvertimePayabilityResolver(
                new ConservativeOvertimePayabilityPolicy());

        PayrollEmployeeCalculationInput input =
            new(
                employeeId,
                "EMP001",
                "Nguyễn Văn An",
                [
                    Day(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            10),
                        120),

                    Day(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            12),
                        60)
                ],
                [],
                [
                    Overtime(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            12),
                        90),

                    Overtime(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            10),
                        120)
                ]);

        IReadOnlyList<OvertimePayabilityResolution>
            results =
                resolver.Resolve(
                    input);

        Assert.Equal(
            2,
            results.Count);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                10),
            results[0].WorkDate);

        Assert.Equal(
            120,
            results[0].PayableMinutes);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                12),
            results[1].WorkDate);

        Assert.Equal(
            60,
            results[1].PayableMinutes);
    }

    [Fact]
    public void Resolve_WhenTimesheetDayIsMissing_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        var resolver =
            new EmployeeOvertimePayabilityResolver(
                new ConservativeOvertimePayabilityPolicy());

        PayrollEmployeeCalculationInput input =
            new(
                employeeId,
                "EMP001",
                "Nguyễn Văn An",
                [],
                [],
                [
                    Overtime(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            10),
                        120)
                ]);

        Assert.Throws<InvalidOperationException>(
            () =>
                resolver.Resolve(
                    input));
    }

    private static MonthlyTimesheetDayItem Day(
        Guid employeeId,
        DateOnly workDate,
        int workedMinutes)
    {
        return new MonthlyTimesheetDayItem(
            Guid.NewGuid(),
            employeeId,
            workDate,
            false,
            0,
            AttendanceCalculationStatus.NonWorkingDay,
            workedMinutes,
            0,
            0,
            0,
            "EMP001",
            "Nguyễn Văn An");
    }

    private static ApprovedOvertimePayrollItem Overtime(
        Guid employeeId,
        DateOnly workDate,
        int approvedMinutes)
    {
        return new ApprovedOvertimePayrollItem(
            Guid.NewGuid(),
            employeeId,
            workDate,
            approvedMinutes);
    }
}
