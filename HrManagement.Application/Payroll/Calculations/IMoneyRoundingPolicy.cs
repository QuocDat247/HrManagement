namespace HrManagement.Application.Payroll.Calculations;

public interface IMoneyRoundingPolicy
{
    decimal Round(
        decimal amount,
        string currencyCode);
}
