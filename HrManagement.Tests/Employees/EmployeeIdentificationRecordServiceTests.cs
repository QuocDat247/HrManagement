using HrManagement.Application.Employees;
using HrManagement.Application.Employees.Profiles;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeIdentificationRecordServiceTests
{
    [Fact]
    public async Task GetRecordsAsync_WhenRecordsExist_MapsDetails()
    {
        Employee employee =
            CreateEmployee();

        var records =
            new[]
            {
                new EmployeeIdentificationRecord(
                    Guid.NewGuid(),
                    employee.Id,
                    EmployeeIdentificationType.NationalId,
                    "012345678901",
                    issueDate:
                        new DateOnly(
                            2024,
                            1,
                            10),
                    expiryDate:
                        new DateOnly(
                            2034,
                            1,
                            10),
                    issuingAuthority:
                        "Cơ quan A",
                    placeOfIssue:
                        "Hà Nội",
                    issuingCountry:
                        "Việt Nam"),

                new EmployeeIdentificationRecord(
                    Guid.NewGuid(),
                    employee.Id,
                    EmployeeIdentificationType.Passport,
                    "P1234567")
            };

        var employeeRepository =
            new StubEmployeeRepository(
                employee);

        var recordRepository =
            new StubIdentificationRecordRepository(
                records);

        var service =
            new EmployeeIdentificationRecordService(
                employeeRepository,
                recordRepository);

        IReadOnlyList<EmployeeIdentificationRecordDetails> result =
            await service.GetRecordsAsync(
                employee.Id);

        Assert.Equal(
            2,
            result.Count);

        Assert.Equal(
            records[0].Id,
            result[0].Id);

        Assert.Equal(
            EmployeeIdentificationType.NationalId,
            result[0].Type);

        Assert.Equal(
            "012345678901",
            result[0].DocumentNumber);

        Assert.Equal(
            new DateOnly(
                2024,
                1,
                10),
            result[0].IssueDate);

        Assert.Equal(
            new DateOnly(
                2034,
                1,
                10),
            result[0].ExpiryDate);

        Assert.Equal(
            "Cơ quan A",
            result[0].IssuingAuthority);

        Assert.Equal(
            "Hà Nội",
            result[0].PlaceOfIssue);

        Assert.Equal(
            "Việt Nam",
            result[0].IssuingCountry);

        Assert.Equal(
            records[1].Id,
            result[1].Id);

        Assert.Equal(
            EmployeeIdentificationType.Passport,
            result[1].Type);

        Assert.Equal(
            "P1234567",
            result[1].DocumentNumber);

        Assert.Equal(
            1,
            recordRepository.GetCallCount);
    }

    [Fact]
    public async Task GetRecordsAsync_WhenEmployeeMissing_Throws()
    {
        Guid employeeId =
            Guid.NewGuid();

        var employeeRepository =
            new StubEmployeeRepository(
                employee: null);

        var recordRepository =
            new StubIdentificationRecordRepository();

        var service =
            new EmployeeIdentificationRecordService(
                employeeRepository,
                recordRepository);

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () =>
                    service.GetRecordsAsync(
                        employeeId));

        Assert.Equal(
            "Nhân viên không tồn tại.",
            exception.Message);

        Assert.Equal(
            0,
            recordRepository.GetCallCount);
    }

    [Fact]
    public async Task SaveRecordAsync_WhenNewAndValid_CreatesAndUpsertsRecord()
    {
        Employee employee =
            CreateEmployee();

        var employeeRepository =
            new StubEmployeeRepository(
                employee);

        var recordRepository =
            new StubIdentificationRecordRepository();

        var service =
            new EmployeeIdentificationRecordService(
                employeeRepository,
                recordRepository);

        var request =
            new SaveEmployeeIdentificationRecordRequest(
                employee.Id,
                RecordId: null,
                EmployeeIdentificationType.Passport,
                "  P1234567  ",
                IssueDate:
                    new DateOnly(
                        2025,
                        1,
                        1),
                ExpiryDate:
                    new DateOnly(
                        2035,
                        1,
                        1),
                IssuingAuthority:
                    "  Cục Quản lý xuất nhập cảnh  ",
                PlaceOfIssue:
                    "  Hà Nội  ",
                IssuingCountry:
                    "  Việt Nam  ");

        EmployeeIdentificationRecordOperationResult result =
            await service.SaveRecordAsync(
                request);

        Assert.True(
            result.IsSuccessful);

        Assert.Null(
            result.ErrorMessage);

        Assert.True(
            result.RecordId.HasValue);

        Assert.NotEqual(
            Guid.Empty,
            result.RecordId!.Value);

        Assert.Equal(
            1,
            recordRepository.UpsertCallCount);

        Assert.NotNull(
            recordRepository.SavedRecord);

        EmployeeIdentificationRecord saved =
            recordRepository.SavedRecord!;

        Assert.Equal(
            result.RecordId.Value,
            saved.Id);

        Assert.Equal(
            employee.Id,
            saved.EmployeeId);

        Assert.Equal(
            EmployeeIdentificationType.Passport,
            saved.Type);

        Assert.Equal(
            "P1234567",
            saved.DocumentNumber);

        Assert.Equal(
            "Cục Quản lý xuất nhập cảnh",
            saved.IssuingAuthority);

        Assert.Equal(
            "Hà Nội",
            saved.PlaceOfIssue);

        Assert.Equal(
            "Việt Nam",
            saved.IssuingCountry);
    }

    [Fact]
    public async Task SaveRecordAsync_WhenExistingAndValid_PreservesRecordId()
    {
        Employee employee =
            CreateEmployee();

        Guid recordId =
            Guid.NewGuid();

        var existing =
            new EmployeeIdentificationRecord(
                recordId,
                employee.Id,
                EmployeeIdentificationType.Other,
                "OLD-001");

        var employeeRepository =
            new StubEmployeeRepository(
                employee);

        var recordRepository =
            new StubIdentificationRecordRepository(
                new[]
                {
                    existing
                });

        var service =
            new EmployeeIdentificationRecordService(
                employeeRepository,
                recordRepository);

        var request =
            new SaveEmployeeIdentificationRecordRequest(
                employee.Id,
                recordId,
                EmployeeIdentificationType.NationalId,
                "012345678901",
                IssueDate:
                    new DateOnly(
                        2024,
                        6,
                        1),
                ExpiryDate:
                    new DateOnly(
                        2034,
                        6,
                        1),
                IssuingAuthority:
                    "Cơ quan mới",
                PlaceOfIssue:
                    "TP. Hồ Chí Minh",
                IssuingCountry:
                    "Việt Nam");

        EmployeeIdentificationRecordOperationResult result =
            await service.SaveRecordAsync(
                request);

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            recordId,
            result.RecordId);

        Assert.Equal(
            1,
            recordRepository.GetCallCount);

        Assert.Equal(
            1,
            recordRepository.UpsertCallCount);

        Assert.NotNull(
            recordRepository.SavedRecord);

        Assert.Equal(
            recordId,
            recordRepository.SavedRecord!.Id);

        Assert.Equal(
            EmployeeIdentificationType.NationalId,
            recordRepository.SavedRecord.Type);

        Assert.Equal(
            "012345678901",
            recordRepository.SavedRecord.DocumentNumber);
    }

    [Fact]
    public async Task SaveRecordAsync_WhenExistingRecordDoesNotBelongToEmployee_ReturnsFailure()
    {
        Employee employee =
            CreateEmployee();

        Guid unknownRecordId =
            Guid.NewGuid();

        var employeeRepository =
            new StubEmployeeRepository(
                employee);

        var recordRepository =
            new StubIdentificationRecordRepository();

        var service =
            new EmployeeIdentificationRecordService(
                employeeRepository,
                recordRepository);

        var request =
            new SaveEmployeeIdentificationRecordRequest(
                employee.Id,
                unknownRecordId,
                EmployeeIdentificationType.NationalId,
                "012345678901",
                IssueDate: null,
                ExpiryDate: null,
                IssuingAuthority: null,
                PlaceOfIssue: null,
                IssuingCountry: null);

        EmployeeIdentificationRecordOperationResult result =
            await service.SaveRecordAsync(
                request);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Giấy tờ định danh không tồn tại.",
            result.ErrorMessage);

        Assert.Null(
            result.RecordId);

        Assert.Equal(
            1,
            recordRepository.GetCallCount);

        Assert.Equal(
            0,
            recordRepository.UpsertCallCount);
    }

    [Theory]
    [InlineData(0, "", "Loại giấy tờ không hợp lệ.")]
    [InlineData(12345, "ABC123", "Loại giấy tờ không hợp lệ.")]
    [InlineData(1, "", "Số giấy tờ là bắt buộc.")]
    public async Task SaveRecordAsync_WhenInputInvalid_ReturnsFailure(
        int rawType,
        string documentNumber,
        string expectedError)
    {
        Employee employee =
            CreateEmployee();

        var employeeRepository =
            new StubEmployeeRepository(
                employee);

        var recordRepository =
            new StubIdentificationRecordRepository();

        var service =
            new EmployeeIdentificationRecordService(
                employeeRepository,
                recordRepository);

        var request =
            new SaveEmployeeIdentificationRecordRequest(
                employee.Id,
                RecordId: null,
                (EmployeeIdentificationType)rawType,
                documentNumber,
                IssueDate: null,
                ExpiryDate: null,
                IssuingAuthority: null,
                PlaceOfIssue: null,
                IssuingCountry: null);

        EmployeeIdentificationRecordOperationResult result =
            await service.SaveRecordAsync(
                request);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            expectedError,
            result.ErrorMessage);

        Assert.Equal(
            0,
            recordRepository.UpsertCallCount);
    }

    [Fact]
    public async Task SaveRecordAsync_WhenExpiryBeforeIssueDate_ReturnsFailure()
    {
        Employee employee =
            CreateEmployee();

        var employeeRepository =
            new StubEmployeeRepository(
                employee);

        var recordRepository =
            new StubIdentificationRecordRepository();

        var service =
            new EmployeeIdentificationRecordService(
                employeeRepository,
                recordRepository);

        var request =
            new SaveEmployeeIdentificationRecordRequest(
                employee.Id,
                RecordId: null,
                EmployeeIdentificationType.Passport,
                "P1234567",
                IssueDate:
                    new DateOnly(
                        2030,
                        1,
                        1),
                ExpiryDate:
                    new DateOnly(
                        2029,
                        12,
                        31),
                IssuingAuthority: null,
                PlaceOfIssue: null,
                IssuingCountry: null);

        EmployeeIdentificationRecordOperationResult result =
            await service.SaveRecordAsync(
                request);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Ngày hết hạn không được trước ngày cấp.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            recordRepository.UpsertCallCount);
    }

    [Fact]
    public async Task SaveRecordAsync_WhenEmployeeMissing_ReturnsFailure()
    {
        Guid employeeId =
            Guid.NewGuid();

        var employeeRepository =
            new StubEmployeeRepository(
                employee: null);

        var recordRepository =
            new StubIdentificationRecordRepository();

        var service =
            new EmployeeIdentificationRecordService(
                employeeRepository,
                recordRepository);

        var request =
            new SaveEmployeeIdentificationRecordRequest(
                employeeId,
                RecordId: null,
                EmployeeIdentificationType.NationalId,
                "012345678901",
                IssueDate: null,
                ExpiryDate: null,
                IssuingAuthority: null,
                PlaceOfIssue: null,
                IssuingCountry: null);

        EmployeeIdentificationRecordOperationResult result =
            await service.SaveRecordAsync(
                request);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Nhân viên không tồn tại.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            recordRepository.UpsertCallCount);
    }

    [Fact]
    public async Task DeleteRecordAsync_WhenValid_DeletesRequestedRecord()
    {
        Employee employee =
            CreateEmployee();

        Guid recordId =
            Guid.NewGuid();

        var employeeRepository =
            new StubEmployeeRepository(
                employee);

        var recordRepository =
            new StubIdentificationRecordRepository();

        var service =
            new EmployeeIdentificationRecordService(
                employeeRepository,
                recordRepository);

        EmployeeIdentificationRecordOperationResult result =
            await service.DeleteRecordAsync(
                employee.Id,
                recordId);

        Assert.True(
            result.IsSuccessful);

        Assert.Null(
            result.ErrorMessage);

        Assert.Equal(
            1,
            recordRepository.DeleteCallCount);

        Assert.Equal(
            employee.Id,
            recordRepository.DeletedEmployeeId);

        Assert.Equal(
            recordId,
            recordRepository.DeletedRecordId);
    }

    [Fact]
    public async Task DeleteRecordAsync_WhenEmployeeMissing_ReturnsFailure()
    {
        Guid employeeId =
            Guid.NewGuid();

        Guid recordId =
            Guid.NewGuid();

        var employeeRepository =
            new StubEmployeeRepository(
                employee: null);

        var recordRepository =
            new StubIdentificationRecordRepository();

        var service =
            new EmployeeIdentificationRecordService(
                employeeRepository,
                recordRepository);

        EmployeeIdentificationRecordOperationResult result =
            await service.DeleteRecordAsync(
                employeeId,
                recordId);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Nhân viên không tồn tại.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            recordRepository.DeleteCallCount);
    }

    private static Employee CreateEmployee()
    {
        return new Employee(
            Guid.NewGuid(),
            "EMP-ID-001",
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

    private sealed class StubIdentificationRecordRepository
        : IEmployeeIdentificationRecordRepository
    {
        private readonly List<EmployeeIdentificationRecord>
            _records;

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

        public EmployeeIdentificationRecord?
            SavedRecord
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
            DeletedRecordId
        {
            get;
            private set;
        }

        public StubIdentificationRecordRepository(
            IEnumerable<EmployeeIdentificationRecord>? records = null)
        {
            _records =
                records?.ToList()
                ?? [];
        }

        public Task<IReadOnlyList<EmployeeIdentificationRecord>>
            GetByEmployeeIdAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            GetCallCount++;

            IReadOnlyList<EmployeeIdentificationRecord> result =
                _records
                    .Where(
                        record =>
                            record.EmployeeId ==
                            employeeId)
                    .ToList();

            return Task.FromResult(
                result);
        }

        public Task UpsertAsync(
            EmployeeIdentificationRecord record,
            CancellationToken cancellationToken = default)
        {
            UpsertCallCount++;

            SavedRecord =
                record;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(
            Guid employeeId,
            Guid recordId,
            CancellationToken cancellationToken = default)
        {
            DeleteCallCount++;

            DeletedEmployeeId =
                employeeId;

            DeletedRecordId =
                recordId;

            return Task.CompletedTask;
        }
    }
}
