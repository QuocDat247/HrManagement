using HrManagement.Application.Employees.OrganizationAssignments;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeOrganizationHistoryViewModelTests
{
    [Fact]
    public async Task LoadAsync_WhenHistoryExists_MapsAssignmentsForDisplay()
    {
        Guid employeeId =
            Guid.NewGuid();

        var employee =
            new Employee(
                employeeId,
                "EMP-HISTORY-001",
                "Nguyễn Văn An",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Nghiên cứu và phát triển",
                "Trưởng nhóm kỹ thuật",
                EmployeeStatus.Active,
                departmentId:
                    Guid.NewGuid(),
                positionId:
                    Guid.NewGuid());

        var service =
            new StubOrganizationHistoryService
            {
                Details =
                    new EmployeeOrganizationAssignmentHistoryDetails(
                        employeeId,
                        [
                            new OrganizationAssignmentHistoryItem(
                                Id:
                                    Guid.NewGuid(),

                                EmploymentPeriodId:
                                    Guid.NewGuid(),

                                SequenceNumber:
                                    1,

                                DepartmentId:
                                    Guid.NewGuid(),

                                DepartmentCode:
                                    "DEV",

                                DepartmentName:
                                    "Phát triển phần mềm cũ",

                                PositionId:
                                    Guid.NewGuid(),

                                PositionCode:
                                    "SWE",

                                PositionName:
                                    "Kỹ sư phần mềm cũ",

                                StartDate:
                                    new DateOnly(
                                        2024,
                                        1,
                                        1),

                                EndDate:
                                    new DateOnly(
                                        2025,
                                        5,
                                        31),

                                IsOpen:
                                    false,

                                IsBaseline:
                                    true),

                            new OrganizationAssignmentHistoryItem(
                                Id:
                                    Guid.NewGuid(),

                                EmploymentPeriodId:
                                    Guid.NewGuid(),

                                SequenceNumber:
                                    2,

                                DepartmentId:
                                    Guid.NewGuid(),

                                DepartmentCode:
                                    "RD",

                                DepartmentName:
                                    "Nghiên cứu và phát triển",

                                PositionId:
                                    Guid.NewGuid(),

                                PositionCode:
                                    "LEAD",

                                PositionName:
                                    "Trưởng nhóm kỹ thuật",

                                StartDate:
                                    new DateOnly(
                                        2025,
                                        6,
                                        1),

                                EndDate:
                                    null,

                                IsOpen:
                                    true,

                                IsBaseline:
                                    false)
                        ])
            };

        var viewModel =
            new EmployeeOrganizationHistoryViewModel(
                service);

        await viewModel.LoadAsync(
            employee);

        Assert.Equal(
            "EMP-HISTORY-001",
            viewModel.EmployeeCode);

        Assert.Equal(
            "Nguyễn Văn An",
            viewModel.FullName);

        Assert.True(
            viewModel.HasAssignments);

        Assert.False(
            viewModel.IsLoading);

        Assert.False(
            viewModel.HasError);

        Assert.Null(
            viewModel.ErrorMessage);

        Assert.Equal(
            2,
            viewModel.Assignments.Count);

        OrganizationAssignmentHistoryViewItem baseline =
            viewModel.Assignments[0];

        Assert.Equal(
            1,
            baseline.SequenceNumber);

        Assert.Equal(
            "Phân công 1 — Dữ liệu nền",
            baseline.Title);

        Assert.Equal(
            "01/01/2024 → 31/05/2025",
            baseline.DateRange);

        Assert.Equal(
            "DEV — Phát triển phần mềm cũ",
            baseline.DepartmentText);

        Assert.Equal(
            "SWE — Kỹ sư phần mềm cũ",
            baseline.PositionText);

        Assert.Equal(
            "Đã kết thúc",
            baseline.StatusText);

        Assert.True(
            baseline.IsBaseline);

        Assert.False(
            baseline.IsOpen);

        Assert.NotNull(
            baseline.BaselineNote);

        OrganizationAssignmentHistoryViewItem current =
            viewModel.Assignments[1];

        Assert.Equal(
            2,
            current.SequenceNumber);

        Assert.Equal(
            "Phân công 2",
            current.Title);

        Assert.Equal(
            "01/06/2025 → Hiện tại",
            current.DateRange);

        Assert.Equal(
            "RD — Nghiên cứu và phát triển",
            current.DepartmentText);

        Assert.Equal(
            "LEAD — Trưởng nhóm kỹ thuật",
            current.PositionText);

        Assert.Equal(
            "Hiện tại",
            current.StatusText);

        Assert.False(
            current.IsBaseline);

        Assert.True(
            current.IsOpen);

        Assert.Null(
            current.BaselineNote);
    }

    [Fact]
    public async Task LoadAsync_WhenHistoryIsEmpty_ShowsEmptyState()
    {
        Guid employeeId =
            Guid.NewGuid();

        var employee =
            CreateEmployee(
                employeeId);

        var service =
            new StubOrganizationHistoryService
            {
                Details =
                    new EmployeeOrganizationAssignmentHistoryDetails(
                        employeeId,
                        [])
            };

        var viewModel =
            new EmployeeOrganizationHistoryViewModel(
                service);

        await viewModel.LoadAsync(
            employee);

        Assert.Empty(
            viewModel.Assignments);

        Assert.False(
            viewModel.HasAssignments);

        Assert.False(
            viewModel.IsLoading);

        Assert.False(
            viewModel.HasError);

        Assert.Null(
            viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_WhenServiceFails_ShowsErrorState()
    {
        var employee =
            CreateEmployee(
                Guid.NewGuid());

        var service =
            new FailingOrganizationHistoryService();

        var viewModel =
            new EmployeeOrganizationHistoryViewModel(
                service);

        await viewModel.LoadAsync(
            employee);

        Assert.Empty(
            viewModel.Assignments);

        Assert.False(
            viewModel.HasAssignments);

        Assert.False(
            viewModel.IsLoading);

        Assert.True(
            viewModel.HasError);

        Assert.Equal(
            "Không thể tải lịch sử phân công tổ chức.",
            viewModel.ErrorMessage);
    }

    private static Employee CreateEmployee(
        Guid employeeId)
    {
        return new Employee(
            employeeId,
            "EMP-HISTORY-TEST",
            "Nhân viên kiểm thử",
            null,
            null,
            null,
            new DateOnly(2024, 1, 1),
            "Phát triển phần mềm",
            "Kỹ sư phần mềm",
            EmployeeStatus.Active,
            departmentId:
                Guid.NewGuid(),
            positionId:
                Guid.NewGuid());
    }

    private sealed class StubOrganizationHistoryService
        : IEmployeeOrganizationHistoryService
    {
        public EmployeeOrganizationAssignmentHistoryDetails?
            Details
        {
            get;
            set;
        }

        public Task<EmployeeOrganizationAssignmentHistoryDetails>
            GetHistoryAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Details
                ?? new EmployeeOrganizationAssignmentHistoryDetails(
                    employeeId,
                    []));
        }
    }

    private sealed class FailingOrganizationHistoryService
        : IEmployeeOrganizationHistoryService
    {
        public Task<EmployeeOrganizationAssignmentHistoryDetails>
            GetHistoryAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromException<
                EmployeeOrganizationAssignmentHistoryDetails>(
                    new InvalidOperationException(
                        "Test failure."));
        }
    }
}
