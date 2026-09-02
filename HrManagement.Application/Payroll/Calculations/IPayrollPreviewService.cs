namespace HrManagement.Application.Payroll.Calculations;

public interface IPayrollPreviewService
{
    Task<PayrollPreview> GetAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);
}
