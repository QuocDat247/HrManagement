using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Application.Employees.Profiles;

public interface IEmployeePersonalProfileRepository
{
    Task<EmployeePersonalProfile?>
        GetByEmployeeIdAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);

    Task UpsertAsync(
        EmployeePersonalProfile profile,
        CancellationToken cancellationToken = default);
}
