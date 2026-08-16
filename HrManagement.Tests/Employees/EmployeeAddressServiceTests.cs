using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HrManagement.Application.Employees;
using HrManagement.Application.Employees.Profiles;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Tests.Employees;
public sealed class EmployeeAddressServiceTests
{
    private sealed class StubEmployeeRepository
    : IEmployeeRepository
    {
        private readonly Employee?
            _employee;

        public StubEmployeeRepository(
            Employee? employee)
        {
            _employee =
                employee;
        }

        public Task<IReadOnlyList<Employee>>
            GetAllAsync(
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Employee> result =
                _employee is null
                    ? []
                    : [_employee];

            return Task.FromResult(
                result);
        }

        public Task<Employee?>
            GetByIdAsync(
                Guid id,
                CancellationToken cancellationToken = default)
        {
            Employee? result =
                _employee?.Id == id
                    ? _employee
                    : null;

            return Task.FromResult(
                result);
        }

        public Task<Employee?>
            GetByEmployeeCodeAsync(
                string employeeCode,
                CancellationToken cancellationToken = default)
        {
            Employee? result =
                _employee is not null
                && string.Equals(
                    _employee.EmployeeCode,
                    employeeCode,
                    StringComparison.OrdinalIgnoreCase)
                    ? _employee
                    : null;

            return Task.FromResult(
                result);
        }

        public Task AddAsync(
            Employee employee,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            Employee employee,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubAddressRepository
    : IEmployeeAddressRepository
    {
        private readonly List<EmployeeAddress>
            _addresses;

        public EmployeeAddress? SavedAddress
        {
            get;
            private set;
        }

        public int UpsertCallCount
        {
            get;
            private set;
        }

        public Guid? DeletedEmployeeId
        {
            get;
            private set;
        }

        public EmployeeAddressType? DeletedType
        {
            get;
            private set;
        }

        public int DeleteCallCount
        {
            get;
            private set;
        }

        public StubAddressRepository(
            IEnumerable<EmployeeAddress>? addresses = null)
        {
            _addresses =
                addresses?.ToList()
                ?? [];
        }

        public Task<IReadOnlyList<EmployeeAddress>>
            GetByEmployeeIdAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<EmployeeAddress> result =
                _addresses
                    .Where(
                        address =>
                            address.EmployeeId ==
                            employeeId)
                    .ToList();

            return Task.FromResult(
                result);
        }

        public Task UpsertAsync(
            EmployeeAddress address,
            CancellationToken cancellationToken = default)
        {
            UpsertCallCount++;

            SavedAddress =
                address;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(
            Guid employeeId,
            EmployeeAddressType type,
            CancellationToken cancellationToken = default)
        {
            DeleteCallCount++;

            DeletedEmployeeId =
                employeeId;

            DeletedType =
                type;

            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task GetAddressesAsync_MapsPermanentAndCurrent()
    {
        Employee employee =
            CreateEmployee();

        var permanent =
            new EmployeeAddress(
                Guid.NewGuid(),
                employee.Id,
                EmployeeAddressType.Permanent,
                "123 Địa chỉ thường trú",
                province:
                    "Hà Nội");

        var current =
            new EmployeeAddress(
                Guid.NewGuid(),
                employee.Id,
                EmployeeAddressType.Current,
                "456 Địa chỉ hiện tại",
                province:
                    "Đà Nẵng");

        var addressRepository =
            new StubAddressRepository(
                [permanent, current]);

        var service =
            new EmployeeAddressService(
                new StubEmployeeRepository(
                    employee),
                addressRepository);

        EmployeeAddressBookDetails result =
            await service.GetAddressesAsync(
                employee.Id);

        Assert.Equal(
            employee.Id,
            result.EmployeeId);

        Assert.NotNull(
            result.PermanentAddress);

        Assert.Equal(
            permanent.Id,
            result.PermanentAddress!.Id);

        Assert.Equal(
            "123 Địa chỉ thường trú",
            result.PermanentAddress.AddressLine);

        Assert.NotNull(
            result.CurrentAddress);

        Assert.Equal(
            current.Id,
            result.CurrentAddress!.Id);

        Assert.Equal(
            "456 Địa chỉ hiện tại",
            result.CurrentAddress.AddressLine);
    }

    // Không có địa chỉ → trả về hai slot rỗng
    [Fact]
    public async Task GetAddressesAsync_WhenNoAddresses_ReturnsEmptySlots()
    {
        Employee employee =
            CreateEmployee();

        var addressRepository =
            new StubAddressRepository();

        var service =
            new EmployeeAddressService(
                new StubEmployeeRepository(
                    employee),
                addressRepository);

        EmployeeAddressBookDetails result =
            await service.GetAddressesAsync(
                employee.Id);

        Assert.Equal(
            employee.Id,
            result.EmployeeId);

        Assert.Null(
            result.PermanentAddress);

        Assert.Null(
            result.CurrentAddress);
    }

    [Fact]
    public async Task SaveAddressAsync_WhenValid_UpsertsAddress()
    {
        Employee employee =
            CreateEmployee();

        var addressRepository =
            new StubAddressRepository();

        var service =
            new EmployeeAddressService(
                new StubEmployeeRepository(
                    employee),
                addressRepository);

        EmployeeAddressOperationResult result =
            await service.SaveAddressAsync(
                new SaveEmployeeAddressRequest(
                    employee.Id,
                    EmployeeAddressType.Current,
                    "  123 Nguyễn Trãi  ",
                    "  Phường A  ",
                    "  Quận B  ",
                    "  Hà Nội  ",
                    "  Việt Nam  ",
                    "  100000  "));

        Assert.True(
            result.IsSuccessful);

        Assert.NotNull(
            addressRepository.SavedAddress);

        Assert.Equal(
            employee.Id,
            addressRepository
                .SavedAddress!
                .EmployeeId);

        Assert.Equal(
            EmployeeAddressType.Current,
            addressRepository
                .SavedAddress
                .Type);

        Assert.Equal(
            "123 Nguyễn Trãi",
            addressRepository
                .SavedAddress
                .AddressLine);

        Assert.Equal(
            "Việt Nam",
            addressRepository
                .SavedAddress
                .Country);
    }

    // Save thiếu địa chỉ chi tiết → failure, không gọi repository
    [Fact]
    public async Task SaveAddressAsync_WhenAddressLineBlank_ReturnsFailure()
    {
        Employee employee =
            CreateEmployee();

        var addressRepository =
            new StubAddressRepository();

        var service =
            new EmployeeAddressService(
                new StubEmployeeRepository(
                    employee),
                addressRepository);

        EmployeeAddressOperationResult result =
            await service.SaveAddressAsync(
                new SaveEmployeeAddressRequest(
                    employee.Id,
                    EmployeeAddressType.Current,
                    "   ",
                    null,
                    null,
                    null,
                    "Việt Nam",
                    null));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Địa chỉ chi tiết là bắt buộc.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            addressRepository.UpsertCallCount);

        Assert.Null(
            addressRepository.SavedAddress);
    }

    // Save cho Employee không tồn tại → failure
    [Fact]
    public async Task SaveAddressAsync_WhenEmployeeMissing_ReturnsFailure()
    {
        var addressRepository =
            new StubAddressRepository();

        var service =
            new EmployeeAddressService(
                new StubEmployeeRepository(
                    null),
                addressRepository);

        Guid employeeId =
            Guid.NewGuid();

        EmployeeAddressOperationResult result =
            await service.SaveAddressAsync(
                new SaveEmployeeAddressRequest(
                    employeeId,
                    EmployeeAddressType.Permanent,
                    "123 Nguyễn Trãi",
                    null,
                    null,
                    "Hà Nội",
                    "Việt Nam",
                    null));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Không tìm thấy nhân viên.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            addressRepository.UpsertCallCount);

        Assert.Null(
            addressRepository.SavedAddress);
    }

    // Delete hợp lệ → repository nhận đúng Employee + Address Type
    [Fact]
    public async Task DeleteAddressAsync_WhenValid_DeletesRequestedType()
    {
        Employee employee =
            CreateEmployee();

        var addressRepository =
            new StubAddressRepository();

        var service =
            new EmployeeAddressService(
                new StubEmployeeRepository(
                    employee),
                addressRepository);

        EmployeeAddressOperationResult result =
            await service.DeleteAddressAsync(
                employee.Id,
                EmployeeAddressType.Current);

        Assert.True(
            result.IsSuccessful);

        Assert.Null(
            result.ErrorMessage);

        Assert.Equal(
            employee.Id,
            addressRepository.DeletedEmployeeId);

        Assert.Equal(
            EmployeeAddressType.Current,
            addressRepository.DeletedType);
    }

    // Delete cho Employee không tồn tại → failure, không gọi repository
    [Fact]
    public async Task DeleteAddressAsync_WhenEmployeeMissing_ReturnsFailure()
    {
        var addressRepository =
            new StubAddressRepository();

        var service =
            new EmployeeAddressService(
                new StubEmployeeRepository(
                    null),
                addressRepository);

        Guid employeeId =
            Guid.NewGuid();

        EmployeeAddressOperationResult result =
            await service.DeleteAddressAsync(
                employeeId,
                EmployeeAddressType.Permanent);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Không tìm thấy nhân viên.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            addressRepository.DeleteCallCount);

        Assert.Null(
            addressRepository.DeletedEmployeeId);

        Assert.Null(
            addressRepository.DeletedType);
    }

    private static Employee CreateEmployee()
    {
        return new Employee(
            Guid.NewGuid(),
            "EMP-ADDRESS-001",
            "Nguyễn Văn An",
            "an@example.com",
            "0901234567",
            new DateOnly(
                1995,
                5,
                10),
            new DateOnly(
                2025,
                1,
                1),
            "Phòng Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);
    }
}
