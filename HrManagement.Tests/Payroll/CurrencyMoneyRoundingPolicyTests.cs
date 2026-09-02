using HrManagement.Application.Payroll.Calculations;

namespace HrManagement.Tests.Payroll;

public sealed class CurrencyMoneyRoundingPolicyTests
{
    private readonly CurrencyMoneyRoundingPolicy
        _policy =
            new();

    [Fact]
    public void Round_WhenCurrencyIsVnd_RoundsToWholeUnit()
    {
        decimal result =
            _policy.Round(
                1234.5m,
                "VND");

        Assert.Equal(
            1235m,
            result);
    }

    [Fact]
    public void Round_WhenCurrencyIsNotVnd_RoundsToTwoDecimals()
    {
        decimal result =
            _policy.Round(
                1234.567m,
                "USD");

        Assert.Equal(
            1234.57m,
            result);
    }
}
