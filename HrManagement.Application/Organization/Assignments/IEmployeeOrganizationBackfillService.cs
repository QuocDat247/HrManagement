namespace HrManagement.Application.Organization.Assignments;

// interface
public interface IEmployeeOrganizationBackfillService
{
    Task<EmployeeOrganizationBackfillResult> BackfillAsync(
        CancellationToken cancellationToken = default);
}
