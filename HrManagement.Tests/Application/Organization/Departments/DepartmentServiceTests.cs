using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HrManagement.Application.Organization.Departments;
using HrManagement.Domain.Organization.Departments;

namespace HrManagement.Tests.Application.Organization.Departments;
public sealed class DepartmentServiceTests
{
    [Fact]
    public async Task CreateDepartmentAsync_WhenCodeAlreadyExists_ReturnsFailure()
    {
        var existingDepartment =
            new Department(
                Guid.NewGuid(),
                "IT",
                "Công nghệ thông tin");

        var repository =
            new InMemoryDepartmentRepository(
                existingDepartment);

        var service =
            new DepartmentService(repository);

        DepartmentOperationResult result =
            await service.CreateDepartmentAsync(
                new CreateDepartmentRequest(
                    " it ",
                    "Phòng IT khác"));

        Assert.False(result.IsSuccessful);
        Assert.Equal(
            "Mã phòng ban đã tồn tại.",
            result.ErrorMessage);

        Assert.Single(repository.Departments);
    }

    [Fact]
    public async Task UpdateDepartmentAsync_WhenDepartmentIsInactive_PreservesInactiveStatus()
    {
        var department =
            new Department(
                Guid.NewGuid(),
                "HR",
                "Nhân sự",
                false);

        var repository =
            new InMemoryDepartmentRepository(
                department);

        var service =
            new DepartmentService(repository);

        DepartmentOperationResult result =
            await service.UpdateDepartmentAsync(
                new UpdateDepartmentRequest(
                    department.Id,
                    "HRA",
                    "Hành chính Nhân sự"));

        Assert.True(result.IsSuccessful);

        Department updated =
            Assert.Single(repository.Departments);

        Assert.Equal("HRA", updated.Code);
        Assert.Equal(
            "Hành chính Nhân sự",
            updated.Name);

        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task CreateDepartmentAsync_WithValidRequest_CreatesDepartment()
    {
        var repository =
            new InMemoryDepartmentRepository();

        var service =
            new DepartmentService(repository);

        DepartmentOperationResult result =
            await service.CreateDepartmentAsync(
                new CreateDepartmentRequest(
                    " hr ",
                    " Phòng Nhân sự "));

        Assert.True(result.IsSuccessful);
        Assert.Null(result.ErrorMessage);

        Department created =
            Assert.Single(repository.Departments);

        Assert.NotEqual(
            Guid.Empty,
            created.Id);

        Assert.Equal(
            "HR",
            created.Code);

        Assert.Equal(
            "Phòng Nhân sự",
            created.Name);

        Assert.True(
            created.IsActive);
    }

    [Fact]
    public async Task UpdateDepartmentAsync_WhenCodeBelongsToAnotherDepartment_ReturnsFailure()
    {
        var hrDepartment =
            new Department(
                Guid.NewGuid(),
                "HR",
                "Nhân sự");

        var itDepartment =
            new Department(
                Guid.NewGuid(),
                "IT",
                "Công nghệ thông tin");

        var repository =
            new InMemoryDepartmentRepository(
                hrDepartment,
                itDepartment);

        var service =
            new DepartmentService(repository);

        DepartmentOperationResult result =
            await service.UpdateDepartmentAsync(
                new UpdateDepartmentRequest(
                    hrDepartment.Id,
                    " it ",
                    "Nhân sự mới"));

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            "Mã phòng ban đã tồn tại.",
            result.ErrorMessage);

        Department persistedHr =
            repository.Departments
                .Single(department =>
                    department.Id == hrDepartment.Id);

        Assert.Equal(
            "HR",
            persistedHr.Code);

        Assert.Equal(
            "Nhân sự",
            persistedHr.Name);
    }

    [Fact]
    public async Task DeactivateDepartmentAsync_WhenDepartmentIsActive_DeactivatesDepartment()
    {
        var department =
            new Department(
                Guid.NewGuid(),
                "FIN",
                "Tài chính");

        var repository =
            new InMemoryDepartmentRepository(
                department);

        var service =
            new DepartmentService(repository);

        DepartmentOperationResult result =
            await service.DeactivateDepartmentAsync(
                department.Id);

        Assert.True(result.IsSuccessful);
        Assert.Null(result.ErrorMessage);

        Department updated =
            Assert.Single(repository.Departments);

        Assert.Equal(
            department.Id,
            updated.Id);

        Assert.Equal(
            "FIN",
            updated.Code);

        Assert.Equal(
            "Tài chính",
            updated.Name);

        Assert.False(
            updated.IsActive);
    }

    [Fact]
    public async Task ReactivateDepartmentAsync_WhenDepartmentIsInactive_ReactivatesDepartment()
    {
        var department =
            new Department(
                Guid.NewGuid(),
                "OPS",
                "Vận hành",
                false);

        var repository =
            new InMemoryDepartmentRepository(
                department);

        var service =
            new DepartmentService(repository);

        DepartmentOperationResult result =
            await service.ReactivateDepartmentAsync(
                department.Id);

        Assert.True(result.IsSuccessful);
        Assert.Null(result.ErrorMessage);

        Department updated =
            Assert.Single(repository.Departments);

        Assert.Equal(
            department.Id,
            updated.Id);

        Assert.Equal(
            "OPS",
            updated.Code);

        Assert.Equal(
            "Vận hành",
            updated.Name);

        Assert.True(
            updated.IsActive);
    }

    [Fact]
    public async Task DeactivateDepartmentAsync_WhenDepartmentDoesNotExist_ReturnsFailure()
    {
        var repository =
            new InMemoryDepartmentRepository();

        var service =
            new DepartmentService(repository);

        DepartmentOperationResult result =
            await service.DeactivateDepartmentAsync(
                Guid.NewGuid());

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            "Không tìm thấy phòng ban.",
            result.ErrorMessage);

        Assert.Empty(
            repository.Departments);
    }


    private sealed class InMemoryDepartmentRepository
    : IDepartmentRepository
    {
        private readonly List<Department>
            _departments;

        public IReadOnlyList<Department> Departments =>
            _departments;

        public InMemoryDepartmentRepository(
            params Department[] departments)
        {
            _departments =
                departments.ToList();
        }

        public Task<IReadOnlyList<Department>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                IReadOnlyList<Department>>(
                    _departments.ToList());
        }

        public Task<Department?> GetByIdAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _departments.FirstOrDefault(
                    department =>
                        department.Id == departmentId));
        }

        public Task<Department?> GetByCodeAsync(
            string code,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _departments.FirstOrDefault(
                    department =>
                        string.Equals(
                            department.Code,
                            code.Trim(),
                            StringComparison.OrdinalIgnoreCase)));
        }

        public Task AddAsync(
            Department department,
            CancellationToken cancellationToken = default)
        {
            _departments.Add(department);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            Department department,
            CancellationToken cancellationToken = default)
        {
            int index =
                _departments.FindIndex(
                    existing =>
                        existing.Id == department.Id);

            if (index >= 0)
            {
                _departments[index] =
                    department;
            }

            return Task.CompletedTask;
        }
    }
}
