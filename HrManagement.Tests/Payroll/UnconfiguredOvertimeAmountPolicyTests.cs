using HrManagement.Application.Payroll.Calculations;

namespace HrManagement.Tests.Payroll;

public sealed class UnconfiguredOvertimeAmountPolicyTests
{
    private readonly UnconfiguredOvertimeAmountPolicy
        _policy =
            new();

    [Fact]
    public void Calculate_WhenThereIsNoPayableOvertime_ReturnsZero()
    {
        PayrollEmployeeCalculationInput employee =
            CreateEmployeeInput();

        OvertimeAmountCalculationResult result =
            _policy.Calculate(
                2026,
                8,
                employee,
                []);

        Assert.True(
            result.IsCalculated);

        Assert.Equal(
            0m,
            result.Amount);
    }

    [Fact]
    public void Calculate_WhenPayableOvertimeExists_ReturnsNotConfigured()
    {
        Guid employeeId =
            Guid.NewGuid();

        PayrollEmployeeCalculationInput employee =
            CreateEmployeeInput(
                employeeId);

        OvertimeAmountCalculationResult result =
            _policy.Calculate(
                2026,
                8,
                employee,
                [
                    new OvertimePayabilityResolution(
                        Guid.NewGuid(),
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            10),
                        120,
                        90,
                        OvertimePayabilityStatus.Payable,
                        "Test")
                ]);

        Assert.False(
            result.IsCalculated);

        Assert.Null(
            result.Amount);

        Assert.Contains(
            "Chưa cấu hình",
            result.ErrorMessage);
    }

    private static PayrollEmployeeCalculationInput
        CreateEmployeeInput(
            Guid? employeeId = null)
    {
        return new PayrollEmployeeCalculationInput(
            employeeId
                ?? Guid.NewGuid(),
            "EMP001",
            "Nguyễn Văn An",
            [],
            [],
            []);
    }
}
