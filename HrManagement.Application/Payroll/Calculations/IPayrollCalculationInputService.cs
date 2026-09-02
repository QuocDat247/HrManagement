namespace HrManagement.Application.Payroll.Calculations;

public interface IPayrollCalculationInputService
{
    Task<PayrollCalculationInput> GetAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);
}
