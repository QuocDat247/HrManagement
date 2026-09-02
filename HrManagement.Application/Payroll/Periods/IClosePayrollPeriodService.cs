namespace HrManagement.Application.Payroll.Periods;

public interface IClosePayrollPeriodService
{
    Task<ClosePayrollPeriodResult> CloseAsync(
        ClosePayrollPeriodRequest request,
        CancellationToken cancellationToken = default);
}
