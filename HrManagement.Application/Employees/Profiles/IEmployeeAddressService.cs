using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Application.Employees.Profiles;

public interface IEmployeeAddressService
{
    Task<EmployeeAddressBookDetails>
        GetAddressesAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);

    Task<EmployeeAddressOperationResult>
        SaveAddressAsync(
            SaveEmployeeAddressRequest request,
            CancellationToken cancellationToken = default);

    Task<EmployeeAddressOperationResult>
        DeleteAddressAsync(
            Guid employeeId,
            EmployeeAddressType type,
            CancellationToken cancellationToken = default);
}
