namespace HrManagement.Application.Payroll.Compensation;

public interface IEmployeeCompensationContextSource
{
    Task<EmployeeCompensationContext?> GetAsync(
        Guid employeeId,
        DateOnly effectiveFrom,
        CancellationToken cancellationToken = default);
}
