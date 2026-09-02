using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Application.Payroll.Calculations;
using HrManagement.Domain.Attendance.Calculations;

namespace HrManagement.Tests.Payroll;

public sealed class ConservativeOvertimePayabilityPolicyTests
{
    private readonly ConservativeOvertimePayabilityPolicy
        _policy =
            new();

    [Fact]
    public void Resolve_WhenNonWorkingDayAndWorkedMinutesCoverApproval_ReturnsAllApprovedMinutes()
    {
        Guid employeeId =
            Guid.NewGuid();

        OvertimePayabilityResolution result =
            _policy.Resolve(
                Day(
                    employeeId,
                    isWorkingDay:
                        false,
                    expectedMinutes:
                        0,
                    workedMinutes:
                        120),
                Overtime(
                    employeeId,
                    approvedMinutes:
                        90));

        Assert.Equal(
            OvertimePayabilityStatus.Payable,
            result.Status);

        Assert.True(
            result.IsResolved);

        Assert.Equal(
            90,
            result.PayableMinutes);
    }

    [Fact]
    public void Resolve_WhenActualWorkedMinutesAreLowerThanApproved_CapsPayableMinutes()
    {
        Guid employeeId =
            Guid.NewGuid();

        OvertimePayabilityResolution result =
            _policy.Resolve(
                Day(
                    employeeId,
                    false,
                    0,
                    60),
                Overtime(
                    employeeId,
                    120));

        Assert.Equal(
            OvertimePayabilityStatus.Payable,
            result.Status);

        Assert.Equal(
            60,
            result.PayableMinutes);
    }

    [Fact]
    public void Resolve_WhenNoActualWorkOnNonWorkingDay_ReturnsNotPayable()
    {
        Guid employeeId =
            Guid.NewGuid();

        OvertimePayabilityResolution result =
            _policy.Resolve(
                Day(
                    employeeId,
                    false,
                    0,
                    0),
                Overtime(
                    employeeId,
                    120));

        Assert.Equal(
            OvertimePayabilityStatus.NotPayable,
            result.Status);

        Assert.Equal(
            0,
            result.PayableMinutes);

        Assert.True(
            result.IsResolved);
    }

    [Fact]
    public void Resolve_WhenWorkingDay_ReturnsRequiresReviewWithoutPayableMinutes()
    {
        Guid employeeId =
            Guid.NewGuid();

        OvertimePayabilityResolution result =
            _policy.Resolve(
                Day(
                    employeeId,
                    true,
                    480,
                    600),
                Overtime(
                    employeeId,
                    120));

        Assert.Equal(
            OvertimePayabilityStatus.RequiresReview,
            result.Status);

        Assert.Null(
            result.PayableMinutes);

        Assert.False(
            result.IsResolved);
    }

    [Fact]
    public void Resolve_WhenEmployeeDoesNotMatch_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                _policy.Resolve(
                    Day(
                        Guid.NewGuid(),
                        false,
                        0,
                        120),
                    Overtime(
                        Guid.NewGuid(),
                        120)));
    }

    private static MonthlyTimesheetDayItem Day(
        Guid employeeId,
        bool isWorkingDay,
        int expectedMinutes,
        int workedMinutes)
    {
        return new MonthlyTimesheetDayItem(
            Guid.NewGuid(),
            employeeId,
            new DateOnly(
                2026,
                8,
                10),
            isWorkingDay,
            expectedMinutes,
            isWorkingDay
                ? AttendanceCalculationStatus.Present
                : AttendanceCalculationStatus.NonWorkingDay,
            workedMinutes,
            0,
            0,
            0,
            "EMP001",
            "Nguyễn Văn An");
    }

    private static ApprovedOvertimePayrollItem Overtime(
        Guid employeeId,
        int approvedMinutes)
    {
        return new ApprovedOvertimePayrollItem(
            Guid.NewGuid(),
            employeeId,
            new DateOnly(
                2026,
                8,
                10),
            approvedMinutes);
    }
}
