using HrManagement.Domain.Payroll.Snapshots;

namespace HrManagement.Tests.Payroll;

public sealed class PayrollEmployeeSnapshotTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesImmutableSnapshot()
    {
        Guid payrollPeriodId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        var snapshot =
            new PayrollEmployeeSnapshot(
                Guid.NewGuid(),
                payrollPeriodId,
                employeeId,
                " EMP001 ",
                " Nguyễn Văn An ",
                "vnd",
                25_000_000m,
                120,
                90,
                500_000m,
                25_500_000m);

        Assert.Equal(
            payrollPeriodId,
            snapshot.PayrollPeriodId);

        Assert.Equal(
            employeeId,
            snapshot.EmployeeId);

        Assert.Equal(
            "EMP001",
            snapshot.EmployeeCode);

        Assert.Equal(
            "Nguyễn Văn An",
            snapshot.EmployeeFullName);

        Assert.Equal(
            "VND",
            snapshot.CurrencyCode);

        Assert.Equal(
            25_000_000m,
            snapshot.BaseSalaryAmount);

        Assert.Equal(
            120,
            snapshot.ApprovedOvertimeMinutes);

        Assert.Equal(
            90,
            snapshot.PayableOvertimeMinutes);

        Assert.Equal(
            500_000m,
            snapshot.OvertimeAmount);

        Assert.Equal(
            25_500_000m,
            snapshot.GrossAmount);
    }

    [Fact]
    public void Constructor_WhenPayableMinutesExceedApproved_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateSnapshot(
                    approvedMinutes:
                        60,
                    payableMinutes:
                        90));
    }

    [Fact]
    public void Constructor_WhenBaseSalaryIsNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSnapshot(
                    baseSalary:
                        -1m));
    }

    [Fact]
    public void Constructor_WhenOvertimeAmountIsNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSnapshot(
                    overtimeAmount:
                        -1m,
                    grossAmount:
                        24_999_999m));
    }

    [Fact]
    public void Constructor_WhenGrossDoesNotEqualBasePlusOvertime_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateSnapshot(
                    baseSalary:
                        25_000_000m,
                    overtimeAmount:
                        500_000m,
                    grossAmount:
                        25_400_000m));
    }

    [Fact]
    public void Constructor_WhenCurrencyIsInvalid_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new PayrollEmployeeSnapshot(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "EMP001",
                    "Nguyễn Văn An",
                    "VN",
                    25_000_000m,
                    0,
                    0,
                    0m,
                    25_000_000m));
    }

    private static PayrollEmployeeSnapshot
        CreateSnapshot(
            decimal baseSalary = 25_000_000m,
            int approvedMinutes = 120,
            int payableMinutes = 90,
            decimal overtimeAmount = 500_000m,
            decimal? grossAmount = null)
    {
        decimal resolvedGrossAmount =
            grossAmount
            ?? baseSalary
            + overtimeAmount;

        return new PayrollEmployeeSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "EMP001",
            "Nguyễn Văn An",
            "VND",
            baseSalary,
            approvedMinutes,
            payableMinutes,
            overtimeAmount,
            resolvedGrossAmount);
    }
}
