namespace HrManagement.Application.Employees.OrganizationAssignments;

public interface IEmployeeOrganizationTransferService
{
    Task<TransferEmployeeOrganizationResult>
        TransferAsync(
            TransferEmployeeOrganizationRequest request,
            CancellationToken cancellationToken = default);
}
