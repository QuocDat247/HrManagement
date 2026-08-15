using HrManagement.Application.Organization.Memberships;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;
using Xunit;

namespace HrManagement.Tests.ViewModels;

public sealed class OrganizationEmployeesViewModelTests
{
    [Fact]
    public void ConfigureForDepartment_SetsDepartmentContext()
    {
        var service =
            new StubMembershipQueryService();

        var viewModel =
            new OrganizationEmployeesViewModel(
                service);

        var department =
            new Department(
                Guid.NewGuid(),
                "DEV",
                "Phát triển phần mềm");

        viewModel.ConfigureForDepartment(
            department);

        Assert.Equal(
            "Phát triển phần mềm",
            viewModel.ContextName);

        Assert.Equal(
            "DEV",
            viewModel.ContextCode);

        Assert.Contains(
            "phòng ban",
            viewModel.WindowTitle,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            department.Name,
            viewModel.EmptyMessage);
    }

    [Fact]
    public void ConfigureForPosition_SetsPositionContext()
    {
        var service =
            new StubMembershipQueryService();

        var viewModel =
            new OrganizationEmployeesViewModel(
                service);

        var position =
            new Position(
                Guid.NewGuid(),
                "DEV",
                "Lập trình viên");

        viewModel.ConfigureForPosition(
            position);

        Assert.Equal(
            "Lập trình viên",
            viewModel.ContextName);

        Assert.Equal(
            "DEV",
            viewModel.ContextCode);

        Assert.Contains(
            "chức danh",
            viewModel.WindowTitle,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            position.Name,
            viewModel.EmptyMessage);
    }

    [Fact]
    public async Task LoadAsync_ForDepartment_UsesDepartmentQueryAndMapsEmployees()
    {
        Guid departmentId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        var service =
            new StubMembershipQueryService
            {
                DepartmentEmployees =
                [
                    new OrganizationEmployeeListItem(
                        employeeId,
                        "EMP001",
                        "Nguyễn Văn An",
                        "Phát triển phần mềm",
                        "Lập trình viên",
                        EmployeeStatus.Active,
                        new DateOnly(2024, 1, 15))
                ]
            };

        var viewModel =
            new OrganizationEmployeesViewModel(
                service);

        viewModel.ConfigureForDepartment(
            new Department(
                departmentId,
                "DEV",
                "Phát triển phần mềm"));

        await viewModel.LoadAsync();

        Assert.Equal(
            departmentId,
            service.LastDepartmentId);

        Assert.Null(
            service.LastPositionId);

        OrganizationEmployeeViewItem item =
            Assert.Single(
                viewModel.Employees);

        Assert.Equal(
            employeeId,
            item.EmployeeId);

        Assert.Equal(
            "EMP001",
            item.EmployeeCode);

        Assert.Equal(
            "Nguyễn Văn An",
            item.FullName);

        Assert.Equal(
            "Đang làm việc",
            item.StatusText);

        Assert.Equal(
            "15/01/2024",
            item.HireDateText);

        Assert.True(
            viewModel.HasEmployees);

        Assert.False(
            viewModel.HasError);
    }

    [Fact]
    public async Task LoadAsync_ForPosition_UsesPositionQuery()
    {
        Guid positionId =
            Guid.NewGuid();

        var service =
            new StubMembershipQueryService
            {
                PositionEmployees =
                [
                    new OrganizationEmployeeListItem(
                        Guid.NewGuid(),
                        "EMP002",
                        "Trần Thị Bình",
                        "Phát triển phần mềm",
                        "Lập trình viên",
                        EmployeeStatus.OnLeave,
                        new DateOnly(2025, 2, 1))
                ]
            };

        var viewModel =
            new OrganizationEmployeesViewModel(
                service);

        viewModel.ConfigureForPosition(
            new Position(
                positionId,
                "DEV",
                "Lập trình viên"));

        await viewModel.LoadAsync();

        Assert.Equal(
            positionId,
            service.LastPositionId);

        Assert.Null(
            service.LastDepartmentId);

        OrganizationEmployeeViewItem item =
            Assert.Single(
                viewModel.Employees);

        Assert.Equal(
            "Nghỉ phép",
            item.StatusText);
    }

    [Fact]
    public async Task LoadAsync_WhenNoEmployees_SetsEmptyState()
    {
        Guid departmentId =
            Guid.NewGuid();

        var service =
            new StubMembershipQueryService();

        var viewModel =
            new OrganizationEmployeesViewModel(
                service);

        viewModel.ConfigureForDepartment(
            new Department(
                departmentId,
                "EMPTY",
                "Phòng ban trống"));

        await viewModel.LoadAsync();

        Assert.Empty(
            viewModel.Employees);

        Assert.False(
            viewModel.HasEmployees);

        Assert.False(
            viewModel.HasError);

        Assert.Null(
            viewModel.ErrorMessage);

        Assert.False(
            viewModel.IsLoading);
    }

    [Fact]
    public async Task LoadAsync_WhenQueryFails_SetsErrorState()
    {
        var service =
            new StubMembershipQueryService
            {
                ExceptionToThrow =
                    new InvalidOperationException(
                        "Database unavailable.")
            };

        var viewModel =
            new OrganizationEmployeesViewModel(
                service);

        viewModel.ConfigureForDepartment(
            new Department(
                Guid.NewGuid(),
                "DEV",
                "Phát triển phần mềm"));

        await viewModel.LoadAsync();

        Assert.Empty(
            viewModel.Employees);

        Assert.False(
            viewModel.HasEmployees);

        Assert.True(
            viewModel.HasError);

        Assert.Equal(
            "Không thể tải danh sách nhân viên.",
            viewModel.ErrorMessage);

        Assert.False(
            viewModel.IsLoading);
    }

    private sealed class StubMembershipQueryService
        : IOrganizationMembershipQueryService
    {
        public IReadOnlyList<OrganizationEmployeeListItem>
            DepartmentEmployees
        {
            get;
            set;
        } =
            Array.Empty<OrganizationEmployeeListItem>();

        public IReadOnlyList<OrganizationEmployeeListItem>
            PositionEmployees
        {
            get;
            set;
        } =
            Array.Empty<OrganizationEmployeeListItem>();

        public Guid? LastDepartmentId
        {
            get;
            private set;
        }

        public Guid? LastPositionId
        {
            get;
            private set;
        }

        public Exception? ExceptionToThrow
        {
            get;
            set;
        }

        public Task<IReadOnlyList<OrganizationStaffingCount>>
            GetDepartmentStaffingCountsAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                IReadOnlyList<OrganizationStaffingCount>>(
                    Array.Empty<OrganizationStaffingCount>());
        }

        public Task<IReadOnlyList<OrganizationStaffingCount>>
            GetPositionStaffingCountsAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                IReadOnlyList<OrganizationStaffingCount>>(
                    Array.Empty<OrganizationStaffingCount>());
        }

        public Task<IReadOnlyList<OrganizationEmployeeListItem>>
            GetEmployeesByDepartmentAsync(
                Guid departmentId,
                CancellationToken cancellationToken = default)
        {
            LastDepartmentId =
                departmentId;

            ThrowIfNeeded();

            return Task.FromResult(
                DepartmentEmployees);
        }

        public Task<IReadOnlyList<OrganizationEmployeeListItem>>
            GetEmployeesByPositionAsync(
                Guid positionId,
                CancellationToken cancellationToken = default)
        {
            LastPositionId =
                positionId;

            ThrowIfNeeded();

            return Task.FromResult(
                PositionEmployees);
        }

        private void ThrowIfNeeded()
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
        }
    }
}
