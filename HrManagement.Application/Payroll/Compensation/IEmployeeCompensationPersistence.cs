using HrManagement.Domain.Payroll.Compensation;

namespace HrManagement.Application.Payroll.Compensation;

public interface IEmployeeCompensationPersistence
{
    Task ApplyAsync(
        EmployeeCompensation? closedCompensation,
        EmployeeCompensation newCompensation,
        string actorUserId,
        string actorUsername,
        CancellationToken cancellationToken = default);
}
