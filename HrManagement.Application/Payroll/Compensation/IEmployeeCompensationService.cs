namespace HrManagement.Application.Payroll.Compensation;

public interface IEmployeeCompensationService
{
    Task<SetEmployeeCompensationResult> SetAsync(
        SetEmployeeCompensationRequest request,
        CancellationToken cancellationToken = default);
}
