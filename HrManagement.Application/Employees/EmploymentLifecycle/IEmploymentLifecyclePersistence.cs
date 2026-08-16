using System.Threading.Tasks;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.OrganizationAssignments;

namespace HrManagement.Application.Employees.EmploymentLifecycle;

public interface IEmploymentLifecyclePersistence
{
    Task CreateEmployeeWithPeriodAndAssignmentAsync(
        Employee employee,
        EmploymentPeriod period,
        EmployeeOrganizationAssignment assignment,
        CancellationToken cancellationToken = default);

    Task UpdateEmployeeWithPeriodAndAssignmentAsync(
        Employee employee,
        EmploymentPeriod period,
        EmployeeOrganizationAssignment assignment,
        CancellationToken cancellationToken = default);

    Task UpdateEmployeeWithNewPeriodAndAssignmentAsync(
        Employee employee,
        EmploymentPeriod newPeriod,
        EmployeeOrganizationAssignment newAssignment,
        CancellationToken cancellationToken = default);
}
