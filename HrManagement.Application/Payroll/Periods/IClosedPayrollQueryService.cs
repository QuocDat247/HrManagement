namespace HrManagement.Application.Payroll.Periods;

public interface IClosedPayrollQueryService
{
    Task<ClosedPayrollReadModel?> GetAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);
}
