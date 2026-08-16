using HrManagement.Domain.Employees.OrganizationAssignments;

namespace HrManagement.Application.Employees.OrganizationAssignments;

public sealed class EmployeeOrganizationHistoryService
    : IEmployeeOrganizationHistoryService
{
    private readonly IEmployeeOrganizationHistoryRepository
        _historyRepository;

    public EmployeeOrganizationHistoryService(
        IEmployeeOrganizationHistoryRepository historyRepository)
    {
        _historyRepository =
            historyRepository;
    }

    public async Task<EmployeeOrganizationAssignmentHistoryDetails>
        GetHistoryAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        EmployeeOrganizationHistory history =
            await _historyRepository
                .GetByEmployeeIdAsync(
                    employeeId,
                    cancellationToken);

        List<OrganizationAssignmentHistoryItem> assignments =
            history.Assignments
                .Select(
                    (assignment, index) =>
                        new OrganizationAssignmentHistoryItem(
                            Id:
                                assignment.Id,

                            EmploymentPeriodId:
                                assignment.EmploymentPeriodId,

                            SequenceNumber:
                                index + 1,

                            DepartmentId:
                                assignment.DepartmentId,

                            DepartmentCode:
                                assignment.DepartmentCode,

                            DepartmentName:
                                assignment.DepartmentName,

                            PositionId:
                                assignment.PositionId,

                            PositionCode:
                                assignment.PositionCode,

                            PositionName:
                                assignment.PositionName,

                            StartDate:
                                assignment.StartDate,

                            EndDate:
                                assignment.EndDate,

                            IsOpen:
                                assignment.IsOpen,

                            IsBaseline:
                                assignment.IsBaseline))
                .ToList();

        return new EmployeeOrganizationAssignmentHistoryDetails(
            EmployeeId:
                history.EmployeeId,

            Assignments:
                assignments);
    }
}
