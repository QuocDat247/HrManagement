using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.OrganizationAssignments;

namespace HrManagement.Application.Employees.OrganizationAssignments;

public interface IEmployeeOrganizationTransferPersistence
{
    Task TransferEmployeeOrganizationAsync(
        Employee employee,
        EmployeeOrganizationAssignment closedAssignment,
        EmployeeOrganizationAssignment newAssignment,
        CancellationToken cancellationToken = default);
}
