using HrManagement.Domain.Employees.OrganizationAssignments;

namespace HrManagement.Application.Employees.OrganizationAssignments;

public interface IEmployeeOrganizationHistoryRepository
{
    Task<EmployeeOrganizationHistory>
        GetByEmployeeIdAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);

    Task AddAssignmentAsync(
        EmployeeOrganizationAssignment assignment,
        CancellationToken cancellationToken = default);

    Task UpdateAssignmentAsync(
        EmployeeOrganizationAssignment assignment,
        CancellationToken cancellationToken = default);
}
