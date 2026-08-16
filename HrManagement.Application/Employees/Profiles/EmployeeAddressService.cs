using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Application.Employees.Profiles;

public sealed class EmployeeAddressService
    : IEmployeeAddressService
{
    private readonly IEmployeeRepository
        _employeeRepository;

    private readonly IEmployeeAddressRepository
        _addressRepository;

    public EmployeeAddressService(
        IEmployeeRepository employeeRepository,
        IEmployeeAddressRepository addressRepository)
    {
        _employeeRepository =
            employeeRepository;

        _addressRepository =
            addressRepository;
    }

    public async Task<EmployeeAddressBookDetails>
        GetAddressesAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        Employee? employee =
            await _employeeRepository
                .GetByIdAsync(
                    employeeId,
                    cancellationToken);

        if (employee is null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy nhân viên.");
        }

        IReadOnlyList<EmployeeAddress> addresses =
            await _addressRepository
                .GetByEmployeeIdAsync(
                    employeeId,
                    cancellationToken);

        EmployeeAddressDetails? permanent =
            addresses
                .Where(
                    address =>
                        address.Type ==
                        EmployeeAddressType.Permanent)
                .Select(Map)
                .SingleOrDefault();

        EmployeeAddressDetails? current =
            addresses
                .Where(
                    address =>
                        address.Type ==
                        EmployeeAddressType.Current)
                .Select(Map)
                .SingleOrDefault();

        return new EmployeeAddressBookDetails(
            employeeId,
            permanent,
            current);
    }

    public async Task<EmployeeAddressOperationResult>
        SaveAddressAsync(
            SaveEmployeeAddressRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        if (request.EmployeeId == Guid.Empty)
        {
            return Failure(
                "Mã nhân viên không hợp lệ.");
        }

        if (!Enum.IsDefined(
                request.Type))
        {
            return Failure(
                "Loại địa chỉ không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(
                request.AddressLine))
        {
            return Failure(
                "Địa chỉ chi tiết là bắt buộc.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Country))
        {
            return Failure(
                "Quốc gia là bắt buộc.");
        }

        Employee? employee =
            await _employeeRepository
                .GetByIdAsync(
                    request.EmployeeId,
                    cancellationToken);

        if (employee is null)
        {
            return Failure(
                "Không tìm thấy nhân viên.");
        }

        var address =
            new EmployeeAddress(
                Guid.NewGuid(),
                request.EmployeeId,
                request.Type,
                request.AddressLine,
                request.Ward,
                request.District,
                request.Province,
                request.Country,
                request.PostalCode);

        await _addressRepository
            .UpsertAsync(
                address,
                cancellationToken);

        return new EmployeeAddressOperationResult(
            IsSuccessful:
                true);
    }

    public async Task<EmployeeAddressOperationResult>
        DeleteAddressAsync(
            Guid employeeId,
            EmployeeAddressType type,
            CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            return Failure(
                "Mã nhân viên không hợp lệ.");
        }

        if (!Enum.IsDefined(type))
        {
            return Failure(
                "Loại địa chỉ không hợp lệ.");
        }

        Employee? employee =
            await _employeeRepository
                .GetByIdAsync(
                    employeeId,
                    cancellationToken);

        if (employee is null)
        {
            return Failure(
                "Không tìm thấy nhân viên.");
        }

        await _addressRepository
            .DeleteAsync(
                employeeId,
                type,
                cancellationToken);

        return new EmployeeAddressOperationResult(
            IsSuccessful:
                true);
    }

    private static EmployeeAddressDetails Map(
        EmployeeAddress address)
    {
        return new EmployeeAddressDetails(
            address.Id,
            address.Type,
            address.AddressLine,
            address.Ward,
            address.District,
            address.Province,
            address.Country,
            address.PostalCode);
    }

    private static EmployeeAddressOperationResult Failure(
        string message)
    {
        return new EmployeeAddressOperationResult(
            IsSuccessful:
                false,

            ErrorMessage:
                message);
    }
}
