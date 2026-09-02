namespace HrManagement.Application.Payroll.Compensation;

public interface IEmployeeCompensationAuthorizationPolicy
{
    Task<bool> CanSetAsync(
        EmployeeCompensationAuthorizationRequest request,
        CancellationToken cancellationToken = default);
}
