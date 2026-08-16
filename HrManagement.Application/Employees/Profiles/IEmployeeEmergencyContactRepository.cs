using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Application.Employees.Profiles;

public interface IEmployeeEmergencyContactRepository
{
    Task<IReadOnlyList<EmployeeEmergencyContact>>
        GetByEmployeeIdAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);

    Task UpsertAsync(
        EmployeeEmergencyContact contact,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid employeeId,
        Guid contactId,
        CancellationToken cancellationToken = default);
}
