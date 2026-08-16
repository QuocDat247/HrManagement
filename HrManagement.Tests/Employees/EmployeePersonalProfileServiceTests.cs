using HrManagement.Application.Employees;
using HrManagement.Application.Employees.Profiles;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Tests.Employees;

public sealed class EmployeePersonalProfileServiceTests
{
    [Fact]
    public async Task GetProfileAsync_WhenProfileExists_ReturnsProfile()
    {
        Employee employee =
            CreateEmployee();

        var profile =
            new EmployeePersonalProfile(
                employee.Id,
                "An",
                EmployeeGender.Male,
                "Việt Nam",
                "Hà Nội");

        var profileRepository =
            new StubProfileRepository
            {
                Profile =
                    profile
            };

        var service =
            new EmployeePersonalProfileService(
                new StubEmployeeRepository(
                    employee),
                profileRepository);

        EmployeePersonalProfileDetails result =
            await service.GetProfileAsync(
                employee.Id);

        Assert.True(
            result.HasProfile);

        Assert.Equal(
            employee.Id,
            result.EmployeeId);

        Assert.Equal(
            "An",
            result.PreferredName);

        Assert.Equal(
            EmployeeGender.Male,
            result.Gender);

        Assert.Equal(
            "Việt Nam",
            result.Nationality);

        Assert.Equal(
            "Hà Nội",
            result.PlaceOfBirth);
    }

    [Fact]
    public async Task GetProfileAsync_WhenProfileDoesNotExist_ReturnsEmptyDetails()
    {
        Employee employee =
            CreateEmployee();

        var service =
            new EmployeePersonalProfileService(
                new StubEmployeeRepository(
                    employee),
                new StubProfileRepository());

        EmployeePersonalProfileDetails result =
            await service.GetProfileAsync(
                employee.Id);

        Assert.False(
            result.HasProfile);

        Assert.Equal(
            employee.Id,
            result.EmployeeId);

        Assert.Null(
            result.PreferredName);

        Assert.Null(
            result.Gender);

        Assert.Null(
            result.Nationality);

        Assert.Null(
            result.PlaceOfBirth);
    }

    [Fact]
    public async Task SaveProfileAsync_WhenValid_UpsertsNormalizedProfile()
    {
        Employee employee =
            CreateEmployee();

        var profileRepository =
            new StubProfileRepository();

        var service =
            new EmployeePersonalProfileService(
                new StubEmployeeRepository(
                    employee),
                profileRepository);

        SaveEmployeePersonalProfileResult result =
            await service.SaveProfileAsync(
                new SaveEmployeePersonalProfileRequest(
                    employee.Id,
                    "  An  ",
                    EmployeeGender.Male,
                    "  Việt Nam  ",
                    "  Hà Nội  "));

        Assert.True(
            result.IsSuccessful);

        Assert.Null(
            result.ErrorMessage);

        Assert.NotNull(
            profileRepository.SavedProfile);

        Assert.Equal(
            employee.Id,
            profileRepository
                .SavedProfile!
                .EmployeeId);

        Assert.Equal(
            "An",
            profileRepository
                .SavedProfile
                .PreferredName);

        Assert.Equal(
            "Việt Nam",
            profileRepository
                .SavedProfile
                .Nationality);

        Assert.Equal(
            "Hà Nội",
            profileRepository
                .SavedProfile
                .PlaceOfBirth);

        Assert.Equal(
            1,
            profileRepository.UpsertCallCount);
    }

    [Fact]
    public async Task SaveProfileAsync_WhenEmployeeDoesNotExist_ReturnsFailure()
    {
        var profileRepository =
            new StubProfileRepository();

        var service =
            new EmployeePersonalProfileService(
                new StubEmployeeRepository(
                    null),
                profileRepository);

        SaveEmployeePersonalProfileResult result =
            await service.SaveProfileAsync(
                new SaveEmployeePersonalProfileRequest(
                    Guid.NewGuid(),
                    "Test",
                    null,
                    null,
                    null));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Không tìm thấy nhân viên.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            profileRepository.UpsertCallCount);
    }

    [Fact]
    public async Task SaveProfileAsync_WhenEmployeeIdIsEmpty_ReturnsFailure()
    {
        var profileRepository =
            new StubProfileRepository();

        var service =
            new EmployeePersonalProfileService(
                new StubEmployeeRepository(
                    null),
                profileRepository);

        SaveEmployeePersonalProfileResult result =
            await service.SaveProfileAsync(
                new SaveEmployeePersonalProfileRequest(
                    Guid.Empty,
                    null,
                    null,
                    null,
                    null));

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Mã nhân viên không hợp lệ.",
            result.ErrorMessage);

        Assert.Equal(
            0,
            profileRepository.UpsertCallCount);
    }

    private static Employee CreateEmployee()
    {
        return new Employee(
            Guid.NewGuid(),
            "EMP-PROFILE-001",
            "Nguyễn Văn An",
            null,
            null,
            null,
            new DateOnly(
                2025,
                1,
                1),
            "Phòng kiểm thử",
            "Chuyên viên",
            EmployeeStatus.Active);
    }

    private sealed class StubProfileRepository
        : IEmployeePersonalProfileRepository
    {
        public EmployeePersonalProfile? Profile
        {
            get;
            set;
        }

        public EmployeePersonalProfile? SavedProfile
        {
            get;
            private set;
        }

        public int UpsertCallCount
        {
            get;
            private set;
        }

        public Task<EmployeePersonalProfile?>
            GetByEmployeeIdAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Profile);
        }

        public Task UpsertAsync(
            EmployeePersonalProfile profile,
            CancellationToken cancellationToken = default)
        {
            UpsertCallCount++;

            SavedProfile =
                profile;

            Profile =
                profile;

            return Task.CompletedTask;
        }
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
            return Task.FromResult<Employee?>(
                null);
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
}
