using HrManagement.Domain.Payroll.Compensation;
using HrManagement.Domain.Payroll.Periods;

namespace HrManagement.Tests.Payroll;

public sealed class PayrollDomainFoundationTests
{
    [Fact]
    public void Compensation_Constructor_CreatesOpenEffectiveDatedSalary()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid employmentPeriodId =
            Guid.NewGuid();

        var compensation =
            new EmployeeCompensation(
                Guid.NewGuid(),
                employeeId,
                employmentPeriodId,
                new DateOnly(
                    2026,
                    8,
                    1),
                25_000_000m,
                "vnd");

        Assert.Equal(
            employeeId,
            compensation.EmployeeId);

        Assert.Equal(
            employmentPeriodId,
            compensation.EmploymentPeriodId);

        Assert.Equal(
            25_000_000m,
            compensation.MonthlyBaseSalary);

        Assert.Equal(
            "VND",
            compensation.CurrencyCode);

        Assert.True(
            compensation.IsOpen);

        Assert.Null(
            compensation.EffectiveTo);
    }

    [Fact]
    public void Compensation_WhenMonthlyBaseSalaryIsNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new EmployeeCompensation(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        1),
                    -1m,
                    "VND"));
    }

    [Fact]
    public void Compensation_WhenCurrencyCodeIsInvalid_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeCompensation(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        1),
                    25_000_000m,
                    "VN"));
    }

    [Fact]
    public void Compensation_Close_ClosesEffectiveRange()
    {
        var compensation =
            CreateCompensation();

        compensation.Close(
            new DateOnly(
                2026,
                8,
                31));

        Assert.False(
            compensation.IsOpen);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                31),
            compensation.EffectiveTo);
    }

    [Fact]
    public void Compensation_CloseBeforeEffectiveFrom_Throws()
    {
        var compensation =
            CreateCompensation();

        Assert.Throws<ArgumentException>(
            () =>
                compensation.Close(
                    new DateOnly(
                        2026,
                        7,
                        31)));

        Assert.True(
            compensation.IsOpen);
    }

    [Fact]
    public void PayrollPeriod_Constructor_CreatesOpenPeriodLinkedToTimesheet()
    {
        Guid timesheetPeriodId =
            Guid.NewGuid();

        var period =
            new PayrollPeriod(
                Guid.NewGuid(),
                timesheetPeriodId,
                2026,
                8);

        Assert.Equal(
            timesheetPeriodId,
            period.TimesheetPeriodId);

        Assert.Equal(
            2026,
            period.Year);

        Assert.Equal(
            8,
            period.Month);

        Assert.Equal(
            PayrollPeriodStatus.Open,
            period.Status);

        Assert.False(
            period.IsClosed);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                1),
            period.StartDate);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                31),
            period.EndDate);
    }

    [Fact]
    public void PayrollPeriod_Close_StoresTrustedMetadata()
    {
        var period =
            CreatePayrollPeriod();

        DateTime closedAtUtc =
            Utc(
                12);

        period.Close(
            closedAtUtc,
            " user-1 ",
            " admin ");

        Assert.True(
            period.IsClosed);

        Assert.Equal(
            PayrollPeriodStatus.Closed,
            period.Status);

        Assert.Equal(
            closedAtUtc,
            period.ClosedAtUtc);

        Assert.Equal(
            "user-1",
            period.ClosedByUserId);

        Assert.Equal(
            "admin",
            period.ClosedByUsername);
    }

    [Fact]
    public void PayrollPeriod_CloseTwice_Throws()
    {
        var period =
            CreatePayrollPeriod();

        period.Close(
            Utc(
                12),
            "user-1",
            "admin");

        Assert.Throws<InvalidOperationException>(
            () =>
                period.Close(
                    Utc(
                        13),
                    "user-1",
                    "admin"));
    }

    private static EmployeeCompensation
        CreateCompensation()
    {
        return new EmployeeCompensation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(
                2026,
                8,
                1),
            25_000_000m,
            "VND");
    }

    private static PayrollPeriod
        CreatePayrollPeriod()
    {
        return new PayrollPeriod(
            Guid.NewGuid(),
            Guid.NewGuid(),
            2026,
            8);
    }

    private static DateTime Utc(
        int hour)
    {
        return new DateTime(
            2026,
            8,
            31,
            hour,
            0,
            0,
            DateTimeKind.Utc);
    }
}
