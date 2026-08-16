using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Application.Employees.Profiles;

public interface IEmployeeAddressRepository
{
    Task<IReadOnlyList<EmployeeAddress>>
        GetByEmployeeIdAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);

    Task UpsertAsync(
        EmployeeAddress address,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid employeeId,
        EmployeeAddressType type,
        CancellationToken cancellationToken = default);
}
