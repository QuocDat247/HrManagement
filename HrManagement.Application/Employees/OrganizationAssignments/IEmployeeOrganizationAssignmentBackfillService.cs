namespace HrManagement.Application.Employees.OrganizationAssignments;

public interface IEmployeeOrganizationAssignmentBackfillService
{
    Task<EmployeeOrganizationAssignmentBackfillResult>
        BackfillAsync(
            CancellationToken cancellationToken = default);
}
