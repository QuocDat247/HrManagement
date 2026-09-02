using HrManagement.Application.Payroll.Calculations;
using HrManagement.Application.Payroll.Compensation;

namespace HrManagement.Tests.Payroll;

public sealed class CalendarDayBaseSalaryProrationPolicyTests
{
    private readonly CalendarDayBaseSalaryProrationPolicy
        _policy =
            new();

    [Fact]
    public void Calculate_WhenSingleSegmentCoversFullMonth_ReturnsMonthlySalary()
    {
        Guid employeeId =
            Guid.NewGuid();

        BaseSalaryProrationResult result =
            _policy.Calculate(
                2026,
                8,
                [
                    Segment(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            1),
                        null,
                        25_000_000m)
                ]);

        Assert.Equal(
            "VND",
            result.CurrencyCode);

        Assert.Equal(
            25_000_000m,
            result.TotalAmount);

        BaseSalaryProrationComponent component =
            Assert.Single(
                result.Components);

        Assert.Equal(
            31,
            component.CoveredCalendarDays);

        Assert.Equal(
            31,
            component.PeriodCalendarDays);
    }

    [Fact]
    public void Calculate_WhenSalaryChangesMidMonth_ProrationUsesCalendarDays()
    {
        Guid employeeId =
            Guid.NewGuid();

        BaseSalaryProrationResult result =
            _policy.Calculate(
                2026,
                8,
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
                        25_000_000m),

                    Segment(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            16),
                        null,
                        28_000_000m)
                ]);

        decimal expected =
            25_000_000m
            * 15m
            / 31m
            + 28_000_000m
            * 16m
            / 31m;

        Assert.Equal(
            expected,
            result.TotalAmount);

        Assert.Equal(
            2,
            result.Components.Count);
    }

    [Fact]
    public void Calculate_WhenEmploymentStartsMidMonth_ProrationCoversOnlyEffectiveDays()
    {
        Guid employeeId =
            Guid.NewGuid();

        BaseSalaryProrationResult result =
            _policy.Calculate(
                2026,
                8,
                [
                    Segment(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            16),
                        null,
                        31_000_000m)
                ]);

        Assert.Equal(
            16_000_000m,
            result.TotalAmount);
    }

    [Fact]
    public void Calculate_WhenCurrenciesDiffer_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        Assert.Throws<ArgumentException>(
            () =>
                _policy.Calculate(
                    2026,
                    8,
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
                            25_000_000m,
                            "VND"),

                        Segment(
                            employeeId,
                            new DateOnly(
                                2026,
                                8,
                                16),
                            null,
                            2_000m,
                            "USD")
                    ]));
    }

    [Fact]
    public void Calculate_WhenSegmentsOverlap_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        Assert.Throws<InvalidOperationException>(
            () =>
                _policy.Calculate(
                    2026,
                    8,
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
                                20),
                            25_000_000m),

                        Segment(
                            employeeId,
                            new DateOnly(
                                2026,
                                8,
                                16),
                            null,
                            28_000_000m)
                    ]));
    }

    private static EmployeeCompensationSegment Segment(
        Guid employeeId,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        decimal monthlyBaseSalary,
        string currencyCode = "VND")
    {
        return new EmployeeCompensationSegment(
            Guid.NewGuid(),
            employeeId,
            Guid.NewGuid(),
            effectiveFrom,
            effectiveTo,
            monthlyBaseSalary,
            currencyCode);
    }
}
