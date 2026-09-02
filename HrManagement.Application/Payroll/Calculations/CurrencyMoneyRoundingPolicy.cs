namespace HrManagement.Application.Payroll.Calculations;

public sealed class CurrencyMoneyRoundingPolicy
    : IMoneyRoundingPolicy
{
    public decimal Round(
        decimal amount,
        string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(
                currencyCode))
        {
            throw new ArgumentException(
                "Mã tiền tệ là bắt buộc.",
                nameof(currencyCode));
        }

        string normalizedCurrencyCode =
            currencyCode
                .Trim()
                .ToUpperInvariant();

        int decimalPlaces =
            normalizedCurrencyCode == "VND"
                ? 0
                : 2;

        return decimal.Round(
            amount,
            decimalPlaces,
            MidpointRounding.AwayFromZero);
    }
}
