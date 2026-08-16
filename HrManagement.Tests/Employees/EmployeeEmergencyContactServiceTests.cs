using HrManagement.Application.Employees;
using HrManagement.Application.Employees.Profiles;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeEmergencyContactServiceTests
{
    [Fact]
    public async Task GetContactsAsync_WhenContactsExist_MapsDetails()
    {
        Employee employee =
            CreateEmployee();

        var contacts =
            new[]
            {
                new EmployeeEmergencyContact(
                    Guid.NewGuid(),
                    employee.Id,
                    "Nguyễn Văn Bình",
                    "Cha",
                    "0901000001",
                    "binh@example.com",
                    isPrimary: true),

                new EmployeeEmergencyContact(
                    Guid.NewGuid(),
                    employee.Id,
                    "Nguyễn Thị Hoa",
                    "Mẹ",
                    "0901000002")
            };

        var employeeRepository =
            new StubEmployeeRepository(
                employee);

        var contactRepository =
            new StubEmergencyContactRepository(
                contacts);

        var service =
            new EmployeeEmergencyContactService(
                employeeRepository,
                contactRepository);

        IReadOnlyList<EmployeeEmergencyContactDetails> result =
            await service.GetContactsAsync(
                employee.Id);

        Assert.Equal(
            2,
            result.Count);

        EmployeeEmergencyContactDetails first =
            result[0];

        Assert.Equal(
            contacts[0].Id,
            first.Id);

        Assert.Equal(
            "Nguyễn Văn Bình",
            first.FullName);

        Assert.Equal(
            "Cha",
            first.Relationship);

        Assert.Equal(
            "0901000001",
            first.PhoneNumber);

        Assert.Equal(
            "binh@example.com",
            first.Email);

        Assert.True(
            first.IsPrimary);

        EmployeeEmergencyContactDetails second =
            result[1];

        Assert.Equal(
            contacts[1].Id,
            second.Id);

        Assert.False(
            second.IsPrimary);

        Assert.Equal(
            1,
            contactRepository.GetCallCount);
    }

    [Fact]
    public async Task GetContactsAsync_WhenEmployeeMissing_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        var employeeRepository =
            new StubEmployeeRepository(
                employee: null);

        var contactRepository =
            new StubEmergencyContactRepository();

        var service =
            new EmployeeEmergencyContactService(
                employeeRepository,
                contactRepository);

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () =>
                    service.GetContactsAsync(
                        employeeId));

        Assert.Equal(
            "Nhân viên không tồn tại.",
            exception.Message);

        Assert.Equal(
            0,
            contactRepository.GetCallCount);
    }

    [Fact]
    public async Task SaveContactAsync_WhenNewAndValid_CreatesAndUpsertsContact()
    {
        Employee employee =
            CreateEmployee();

        var employeeRepository =
            new StubEmployeeRepository(
                employee);

        var contactRepository =
            new StubEmergencyContactRepository();

        var service =
            new EmployeeEmergencyContactService(
                employeeRepository,
                contactRepository);

        var request =
            new SaveEmployeeEmergencyContactRequest(
                employee.Id,
                ContactId: null,
                FullName: "  Nguyễn Văn Bình  ",
                Relationship: "  Cha  ",
                PhoneNumber: "  +84 901 234 567  ",
                Email: "  binh@example.com  ",
                IsPrimary: true);

        EmployeeEmergencyContactOperationResult result =
            await service.SaveContactAsync(
                request);

        Assert.True(
            result.IsSuccessful);

        Assert.Null(
            result.ErrorMessage);

        Assert.True(
            result.ContactId.HasValue);

        Assert.NotEqual(
            Guid.Empty,
            result.ContactId!.Value);

        Assert.Equal(
            1,
            contactRepository.UpsertCallCount);

        Assert.NotNull(
            contactRepository.SavedContact);

        EmployeeEmergencyContact saved =
            contactRepository.SavedContact!;

        Assert.Equal(
            result.ContactId.Value,
            saved.Id);

        Assert.Equal(
            employee.Id,
            saved.EmployeeId);

        Assert.Equal(
            "Nguyễn Văn Bình",
            saved.FullName);

        Assert.Equal(
            "Cha",
            saved.Relationship);

        Assert.Equal(
            "+84 901 234 567",
            saved.PhoneNumber);

        Assert.Equal(
            "binh@example.com",
            saved.Email);

        Assert.True(
            saved.IsPrimary);
    }

    [Fact]
    public async Task SaveContactAsync_WhenExistingAndValid_PreservesContactId()
    {
        Employee employee =
            CreateEmployee();

        Guid contactId =
            Guid.NewGuid();

        var existing =
            new EmployeeEmergencyContact(
                contactId,
                employee.Id,
                "Tên cũ",
                "Bạn",
                "0901111111");

        var employeeRepository =
            new StubEmployeeRepository(
                employee);

        var contactRepository =
            new StubEmergencyContactRepository(
                new[]
                {
                    existing
                });

        var service =
            new EmployeeEmergencyContactService(
                employeeRepository,
                contactRepository);

        var request =
            new SaveEmployeeEmergencyContactRequest(
                employee.Id,
                contactId,
                "Tên mới",
                "Anh trai",
                "0902222222",
                "new@example.com",
                IsPrimary: true);

        EmployeeEmergencyContactOperationResult result =
            await service.SaveContactAsync(
                request);

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            contactId,
            result.ContactId);

        Assert.Equal(
            1,
            contactRepository.GetCallCount);

        Assert.Equal(
            1,
            contactRepository.UpsertCallCount);

        Assert.NotNull(
            contactRepository.SavedContact);

        Assert.Equal(
            contactId,
            contactRepository.SavedContact!.Id);

        Assert.Equal(
            "Tên mới",
            contactRepository.SavedContact.FullName);

        Assert.Equal(
            "Anh trai",
            contactRepository.SavedContact.Relationship);

        Assert.Equal(
            "0902222222",
            contactRepository.SavedContact.PhoneNumber);

        Assert.Equal(
            "new@example.com",
            contactRepository.SavedContact.Email);

        Assert.True(
            contactRepository.SavedContact.IsPrimary);
    }

    [Fact]
    public async Task SaveContactAsync_WhenExistingContactDoesNotBelongToEmployee_ReturnsFailure()
    {
        Employee employee =
            CreateEmployee();

        Guid unknownContactId =
            Guid.NewGuid();

        var employeeRepository =
            new StubEmployeeRepository(
                employee);

        var contactRepository =
            new StubEmergencyContactRepository();

        var service =
            new EmployeeEmergencyContactService(
                employeeRepository,
                contactRepository);

        var request =
            new SaveEmployeeEmergencyContactRequest(
                employee.Id,
                unknownContactId,
                "Nguyễn Văn Bình",
                "Cha",
                "0901234567",
                null,
                IsPrimary: false);

        EmployeeEmergencyContactOperationResult result =
            await service.SaveContactAsync(
                request);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Liên hệ khẩn cấp không tồn tại.",
            result.ErrorMessage);

        Assert.Null(
            result.ContactId);

        Assert.Equal(
            1,
            contactRepository.GetCallCount);

        Assert.Equal(
            0,
            contactRepository.UpsertCallCount);
    }

    [Theory]
    [InlineData("", "Cha", "0901234567", "Họ tên người liên hệ là bắt buộc.")]
    [InlineData("Nguyễn Văn Bình", "", "0901234567", "Mối quan hệ là bắt buộc.")]
    [InlineData("Nguyễn Văn Bình", "Cha", "", "Số điện thoại người liên hệ là bắt buộc.")]
    public async Task SaveContactAsync_WhenRequiredFieldBlank_ReturnsFailure(
        string fullName,
        string relationship,
        string phoneNumber,
        string expectedError)
    {
        Employee employee =
            CreateEmployee();

        var employeeRepository =
            new StubEmployeeRepository(
                employee);

        var contactRepository =
            new StubEmergencyContactRepository();

        var service =
            new EmployeeEmergencyContactService(
                employeeRepository,
                contactRepository);

        var request =
            new SaveEmployeeEmergencyContactRequest(
                employee.Id,
                ContactId: null,
                fullName,
                relationship,
                phoneNumber,
                Email: null,
                IsPrimary: false);

        EmployeeEmergencyContactOperationResult result =
            await service.SaveContactAsync(
                request);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            expectedError,
            result.ErrorMessage);

        Assert.Equal(
            0,
            contactRepository.UpsertCallCount);
    }

    [Fact]
    public async Task SaveContactAsync_WhenEmployeeMissing_ReturnsFailure()
    {
        Guid employeeId =
            Guid.NewGuid();

        var employeeRepository =
            new StubEmployeeRepository(
                employee: null);

        var contactRepository =
            new StubEmergencyContactRepository();

        var service =
            new EmployeeEmergencyContactService(
                employeeRepository,
                contactRepository);

        var request =
            new SaveEmployeeEmergencyContactRequest(
                employeeId,
                ContactId: null,
                FullName: "Nguyễn Văn Bình",
                Relationship: "Cha",
                PhoneNumber: "0901234567",
                Email: null,
                IsPrimary: false);

        EmployeeEmergencyContactOperationResult result =
            await service.SaveContactAsync(
                request);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Nhân viên không tồn tại.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            contactRepository.UpsertCallCount);
    }

    [Fact]
    public async Task DeleteContactAsync_WhenValid_DeletesRequestedContact()
    {
        Employee employee =
            CreateEmployee();

        Guid contactId =
            Guid.NewGuid();

        var employeeRepository =
            new StubEmployeeRepository(
                employee);

        var contactRepository =
            new StubEmergencyContactRepository();

        var service =
            new EmployeeEmergencyContactService(
                employeeRepository,
                contactRepository);

        EmployeeEmergencyContactOperationResult result =
            await service.DeleteContactAsync(
                employee.Id,
                contactId);

        Assert.True(
            result.IsSuccessful);

        Assert.Null(
            result.ErrorMessage);

        Assert.Equal(
            1,
            contactRepository.DeleteCallCount);

        Assert.Equal(
            employee.Id,
            contactRepository.DeletedEmployeeId);

        Assert.Equal(
            contactId,
            contactRepository.DeletedContactId);
    }

    [Fact]
    public async Task DeleteContactAsync_WhenEmployeeMissing_ReturnsFailure()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid contactId =
            Guid.NewGuid();

        var employeeRepository =
            new StubEmployeeRepository(
                employee: null);

        var contactRepository =
            new StubEmergencyContactRepository();

        var service =
            new EmployeeEmergencyContactService(
                employeeRepository,
                contactRepository);

        EmployeeEmergencyContactOperationResult result =
            await service.DeleteContactAsync(
                employeeId,
                contactId);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Nhân viên không tồn tại.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            contactRepository.DeleteCallCount);
    }

    private static Employee CreateEmployee()
    {
        return new Employee(
            Guid.NewGuid(),
            "EMP-EMERGENCY-001",
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

    private sealed class StubEmergencyContactRepository
        : IEmployeeEmergencyContactRepository
    {
        private readonly List<EmployeeEmergencyContact>
            _contacts;

        public int GetCallCount
        {
            get;
            private set;
        }

        public int UpsertCallCount
        {
            get;
            private set;
        }

        public int DeleteCallCount
        {
            get;
            private set;
        }

        public EmployeeEmergencyContact?
            SavedContact
        {
            get;
            private set;
        }

        public Guid?
            DeletedEmployeeId
        {
            get;
            private set;
        }

        public Guid?
            DeletedContactId
        {
            get;
            private set;
        }

        public StubEmergencyContactRepository(
            IEnumerable<EmployeeEmergencyContact>? contacts = null)
        {
            _contacts =
                contacts?.ToList()
                ?? [];
        }

        public Task<IReadOnlyList<EmployeeEmergencyContact>>
            GetByEmployeeIdAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            GetCallCount++;

            IReadOnlyList<EmployeeEmergencyContact> result =
                _contacts
                    .Where(
                        contact =>
                            contact.EmployeeId ==
                            employeeId)
                    .ToList();

            return Task.FromResult(
                result);
        }

        public Task UpsertAsync(
            EmployeeEmergencyContact contact,
            CancellationToken cancellationToken = default)
        {
            UpsertCallCount++;

            SavedContact =
                contact;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(
            Guid employeeId,
            Guid contactId,
            CancellationToken cancellationToken = default)
        {
            DeleteCallCount++;

            DeletedEmployeeId =
                employeeId;

            DeletedContactId =
                contactId;

            return Task.CompletedTask;
        }
    }
}
