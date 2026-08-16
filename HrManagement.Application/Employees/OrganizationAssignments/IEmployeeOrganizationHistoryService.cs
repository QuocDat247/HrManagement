namespace HrManagement.Application.Employees.OrganizationAssignments;

public interface IEmployeeOrganizationHistoryService
{
    Task<EmployeeOrganizationAssignmentHistoryDetails>
        GetHistoryAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);
}
