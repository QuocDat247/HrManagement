using HrManagement.Application.Payroll.Calculations;
using HrManagement.Application.Payroll.Compensation;

namespace HrManagement.Tests.Payroll;

public sealed class ConfiguredOvertimeAmountPolicyTests
{
    [Fact]
    public void Calculate_WhenPayableOvertimeExists_UsesConfiguredMinuteRate()
    {
        Guid employeeId =
            Guid.NewGuid();

        var policy =
            CreatePolicy(
                standardMinutes:
                    12_480,
                multiplier:
                    2m);

        PayrollEmployeeCalculationInput employee =
            EmployeeInput(
                employeeId,
                monthlySalary:
                    12_480_000m);

        OvertimeAmountCalculationResult result =
            policy.Calculate(
                2026,
                8,
                employee,
                [
                    Resolution(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            10),
                        120)
                ]);

        Assert.True(
            result.IsCalculated);

        Assert.Equal(
            240_000m,
            result.Amount);
    }

    [Fact]
    public void Calculate_WhenSalaryChanges_UsesSegmentEffectiveOnOvertimeDate()
    {
        Guid employeeId =
            Guid.NewGuid();

        var policy =
            CreatePolicy(
                12_480,
                2m);

        PayrollEmployeeCalculationInput employee =
            new(
                employeeId,
                "EMP001",
                "Nguyễn Văn An",
                [],
                [
                    Segment(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            1),
                        new DateOnly(
                            2026,
                            8,
                            15),
                        12_480_000m),

                    Segment(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            16),
                        null,
                        24_960_000m)
                ],
                []);

        OvertimeAmountCalculationResult result =
            policy.Calculate(
                2026,
                8,
                employee,
                [
                    Resolution(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            10),
                        60),

                    Resolution(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            20),
                        60)
                ]);

        Assert.True(
            result.IsCalculated);

        Assert.Equal(
            360_000m,
            result.Amount);
    }

    [Fact]
    public void Calculate_WhenThereIsNoPayableOvertime_ReturnsZero()
    {
        Guid employeeId =
            Guid.NewGuid();

        var policy =
            CreatePolicy(
                12_480,
                2m);

        OvertimeAmountCalculationResult result =
            policy.Calculate(
                2026,
                8,
                EmployeeInput(
                    employeeId,
                    12_480_000m),
                []);

        Assert.True(
            result.IsCalculated);

        Assert.Equal(
            0m,
            result.Amount);
    }

    [Fact]
    public void Calculate_WhenResolutionRequiresReview_ReturnsFailure()
    {
        Guid employeeId =
            Guid.NewGuid();

        var policy =
            CreatePolicy(
                12_480,
                2m);

        OvertimeAmountCalculationResult result =
            policy.Calculate(
                2026,
                8,
                EmployeeInput(
                    employeeId,
                    12_480_000m),
                [
                    new OvertimePayabilityResolution(
                        Guid.NewGuid(),
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            10),
                        120,
                        null,
                        OvertimePayabilityStatus.RequiresReview,
                        "Test")
                ]);

        Assert.False(
            result.IsCalculated);

        Assert.Null(
            result.Amount);
    }

    private static ConfiguredOvertimeAmountPolicy
        CreatePolicy(
            int standardMinutes,
            decimal multiplier)
    {
        return new ConfiguredOvertimeAmountPolicy(
            new StubProfileSource(
                new PayrollPolicyProfile(
                    standardMinutes,
                    multiplier)));
    }

    private static PayrollEmployeeCalculationInput
        EmployeeInput(
            Guid employeeId,
            decimal monthlySalary)
    {
        return new PayrollEmployeeCalculationInput(
            employeeId,
            "EMP001",
            "Nguyễn Văn An",
            [],
            [
                Segment(
                    employeeId,
                    new DateOnly(
                        2026,
                        8,
                        1),
                    null,
                    monthlySalary)
            ],
            []);
    }

    private static EmployeeCompensationSegment Segment(
        Guid employeeId,
        DateOnly from,
        DateOnly? to,
        decimal salary)
    {
        return new EmployeeCompensationSegment(
            Guid.NewGuid(),
            employeeId,
            Guid.NewGuid(),
            from,
            to,
            salary,
            "VND");
    }

    private static OvertimePayabilityResolution Resolution(
        Guid employeeId,
        DateOnly workDate,
        int payableMinutes)
    {
        return new OvertimePayabilityResolution(
            Guid.NewGuid(),
            employeeId,
            workDate,
            payableMinutes,
            payableMinutes,
            OvertimePayabilityStatus.Payable,
            "Test");
    }

    private sealed class StubProfileSource
        : IPayrollPolicyProfileSource
    {
        private readonly PayrollPolicyProfile
            _profile;

        public StubProfileSource(
            PayrollPolicyProfile profile)
        {
            _profile =
                profile;
        }

        public PayrollPolicyProfile GetCurrent()
        {
            return _profile;
        }
    }
}
