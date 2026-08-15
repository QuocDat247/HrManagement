using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HrManagement.Application.Organization.Departments;
using HrManagement.Desktop.Services.Departments;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Application.Organization.Memberships;

namespace HrManagement.Tests.ViewModels;
public sealed class DepartmentsViewModelTests
{
    [Fact]
    public async Task LoadAsync_LoadsDepartments()
    {
        var departments =
            new[]
            {
            CreateDepartment(
                "HR",
                "Nhân sự"),

            CreateDepartment(
                "IT",
                "Công nghệ thông tin")
            };

        var service =
            new StubDepartmentService(
                departments);

        var dialogService =
            new StubDepartmentDialogService();

        var viewModel =
            new DepartmentsViewModel(
                service,
                dialogService,
                new StubMembershipQueryService());

        await viewModel.LoadAsync();

        Assert.Equal(
            2,
            viewModel.Departments.Count);

        Assert.Null(
            viewModel.ErrorMessage);

        Assert.False(
            viewModel.IsLoading);
    }

    [Fact]
    public async Task LoadAsync_MapsDepartmentStaffingCounts()
    {
        Department dev =
            CreateDepartment(
                "DEV",
                "Phát triển phần mềm");

        Department hr =
            CreateDepartment(
                "HR",
                "Nhân sự");

        var service =
            new StubDepartmentService(
                [dev, hr]);

        var dialogService =
            new StubDepartmentDialogService();

        var membershipService =
            new StubMembershipQueryService
            {
                DepartmentCounts =
                [
                    new OrganizationStaffingCount(
                    dev.Id,
                    ActiveCount: 2,
                    OnLeaveCount: 1,
                    InactiveCount: 4)
                ]
            };

        var viewModel =
            new DepartmentsViewModel(
                service,
                dialogService,
                membershipService);

        await viewModel.LoadAsync();

        DepartmentStaffingViewItem devRow =
            Assert.Single(
                viewModel.DepartmentRows.Where(
                    row =>
                        row.Id == dev.Id));

        Assert.Equal(
            3,
            devRow.CurrentEmployeeCount);

        Assert.Equal(
            7,
            devRow.TotalLinkedEmployeeCount);

        DepartmentStaffingViewItem hrRow =
            Assert.Single(
                viewModel.DepartmentRows.Where(
                    row =>
                        row.Id == hr.Id));

        Assert.Equal(
            0,
            hrRow.CurrentEmployeeCount);

        viewModel.SelectedDepartmentRow =
            devRow;

        Assert.Same(
            dev,
            viewModel.SelectedDepartment);

        Assert.True(
            viewModel.ViewEmployeesCommand
                .CanExecute(null));

        Assert.Equal(
            "3 hiện tại • 4 đã nghỉ",
            devRow.StaffingSummaryText);

        Assert.Equal(
            "0 hiện tại • 0 đã nghỉ",
            hrRow.StaffingSummaryText);

        Assert.Equal(
            "Đang làm việc: 2 • Nghỉ phép: 1 • Đã nghỉ: 4",
            devRow.StaffingDetailText);
    }

    [Fact]
    public async Task AddDepartmentCommand_WhenConfirmed_CallsService()
    {
        var service =
            new StubDepartmentService(
                Array.Empty<Department>());

        var dialogService =
            new StubDepartmentDialogService
            {
                AddResult =
                    new DepartmentEditorDialogResult(
                        "FIN",
                        "Tài chính")
            };

        var viewModel =
            new DepartmentsViewModel(
                service,
                dialogService,
                new StubMembershipQueryService());

        await viewModel
            .AddDepartmentCommand
            .ExecuteAsync(null);

        Assert.NotNull(
            service.CreateRequest);

        Assert.Equal(
            "FIN",
            service.CreateRequest!.Code);

        Assert.Equal(
            "Tài chính",
            service.CreateRequest.Name);
    }

    [Fact]
    public async Task EditDepartmentCommand_WhenConfirmed_CallsServiceForSelectedDepartment()
    {
        Department department =
            CreateDepartment(
                "IT",
                "Công nghệ thông tin");

        var service =
            new StubDepartmentService(
                [department]);

        var dialogService =
            new StubDepartmentDialogService
            {
                EditResult =
                    new DepartmentEditorDialogResult(
                        "TECH",
                        "Công nghệ")
            };

        var viewModel =
            new DepartmentsViewModel(
                service,
                dialogService,
                new StubMembershipQueryService())
            {
                SelectedDepartment =
                    department
            };

        await viewModel
            .EditDepartmentCommand
            .ExecuteAsync(null);

        Assert.NotNull(
            service.UpdateRequest);

        Assert.Equal(
            department.Id,
            service.UpdateRequest!.DepartmentId);

        Assert.Equal(
            "TECH",
            service.UpdateRequest.Code);
    }

    [Fact]
    public void DeactivateDepartmentCommand_CanExecuteOnlyForActiveDepartment()
    {
        var service =
            new StubDepartmentService(
                Array.Empty<Department>());

        var dialogService =
            new StubDepartmentDialogService();

        var viewModel =
            new DepartmentsViewModel(
                service,
                dialogService,
                new StubMembershipQueryService());

        Assert.False(
            viewModel
                .DeactivateDepartmentCommand
                .CanExecute(null));

        viewModel.SelectedDepartment =
            CreateDepartment(
                "HR",
                "Nhân sự",
                true);

        Assert.True(
            viewModel
                .DeactivateDepartmentCommand
                .CanExecute(null));

        viewModel.SelectedDepartment =
            CreateDepartment(
                "OLD",
                "Phòng cũ",
                false);

        Assert.False(
            viewModel
                .DeactivateDepartmentCommand
                .CanExecute(null));
    }

    [Fact]
    public void ReactivateDepartmentCommand_CanExecuteOnlyForInactiveDepartment()
    {
        var service =
            new StubDepartmentService(
                Array.Empty<Department>());

        var dialogService =
            new StubDepartmentDialogService();

        var viewModel =
            new DepartmentsViewModel(
                service,
                dialogService,
                new StubMembershipQueryService());

        viewModel.SelectedDepartment =
            CreateDepartment(
                "HR",
                "Nhân sự",
                true);

        Assert.False(
            viewModel
                .ReactivateDepartmentCommand
                .CanExecute(null));

        viewModel.SelectedDepartment =
            CreateDepartment(
                "OLD",
                "Phòng cũ",
                false);

        Assert.True(
            viewModel
                .ReactivateDepartmentCommand
                .CanExecute(null));
    }

    private static Department CreateDepartment(
    string code,
    string name,
    bool isActive = true)
    {
        return new Department(
            Guid.NewGuid(),
            code,
            name,
            isActive);
    }

    private sealed class StubDepartmentDialogService
    : IDepartmentDialogService
    {
        public Guid? ViewedDepartmentId
        {
            get;
            private set;
        }

        public void ShowEmployees(
            Department department)
        {
            ViewedDepartmentId =
                department.Id;
        }

        public DepartmentEditorDialogResult?
            AddResult
        {
            get;
            set;
        }

        public DepartmentEditorDialogResult?
            EditResult
        {
            get;
            set;
        }

        public bool ConfirmDeactivate
        {
            get;
            set;
        } = true;

        public bool ConfirmReactivate
        {
            get;
            set;
        } = true;

        public DepartmentEditorDialogResult?
            ShowAddDepartmentDialog()
        {
            return AddResult;
        }

        public DepartmentEditorDialogResult?
            ShowEditDepartmentDialog(
                Department department)
        {
            return EditResult;
        }

        public bool ConfirmDeactivateDepartment(
            Department department)
        {
            return ConfirmDeactivate;
        }

        public bool ConfirmReactivateDepartment(
            Department department)
        {
            return ConfirmReactivate;
        }
    }

    private sealed class StubDepartmentService
    : IDepartmentService
    {
        private readonly IReadOnlyList<Department>
            _departments;

        public CreateDepartmentRequest?
            CreateRequest
        {
            get;
            private set;
        }

        public UpdateDepartmentRequest?
            UpdateRequest
        {
            get;
            private set;
        }

        public Guid?
            DeactivatedDepartmentId
        {
            get;
            private set;
        }

        public Guid?
            ReactivatedDepartmentId
        {
            get;
            private set;
        }

        public DepartmentOperationResult
            OperationResult
        {
            get;
            set;
        } =
            new(
                true,
                null);

        public StubDepartmentService(
            IReadOnlyList<Department> departments)
        {
            _departments =
                departments;
        }

        public Task<IReadOnlyList<Department>>
            GetDepartmentsAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _departments);
        }

        public Task<DepartmentOperationResult>
            CreateDepartmentAsync(
                CreateDepartmentRequest request,
                CancellationToken cancellationToken = default)
        {
            CreateRequest =
                request;

            return Task.FromResult(
                OperationResult);
        }

        public Task<DepartmentOperationResult>
            UpdateDepartmentAsync(
                UpdateDepartmentRequest request,
                CancellationToken cancellationToken = default)
        {
            UpdateRequest =
                request;

            return Task.FromResult(
                OperationResult);
        }

        public Task<DepartmentOperationResult>
            DeactivateDepartmentAsync(
                Guid departmentId,
                CancellationToken cancellationToken = default)
        {
            DeactivatedDepartmentId =
                departmentId;

            return Task.FromResult(
                OperationResult);
        }

        public Task<DepartmentOperationResult>
            ReactivateDepartmentAsync(
                Guid departmentId,
                CancellationToken cancellationToken = default)
        {
            ReactivatedDepartmentId =
                departmentId;

            return Task.FromResult(
                OperationResult);
        }
    }

    private sealed class StubMembershipQueryService
    : IOrganizationMembershipQueryService
    {
        public IReadOnlyList<OrganizationStaffingCount>
            DepartmentCounts
        { get; set; } =
                Array.Empty<OrganizationStaffingCount>();

        public Task<IReadOnlyList<OrganizationEmployeeListItem>>
            GetEmployeesByDepartmentAsync(
                Guid departmentId,
                CancellationToken cancellationToken = default)
            => Task.FromResult<
                IReadOnlyList<OrganizationEmployeeListItem>>([]);

        public Task<IReadOnlyList<OrganizationEmployeeListItem>>
            GetEmployeesByPositionAsync(
                Guid positionId,
                CancellationToken cancellationToken = default)
            => Task.FromResult<
                IReadOnlyList<OrganizationEmployeeListItem>>([]);

        public Task<IReadOnlyList<OrganizationStaffingCount>>
            GetDepartmentStaffingCountsAsync(
                CancellationToken cancellationToken = default)
            => Task.FromResult(DepartmentCounts);

        public Task<IReadOnlyList<OrganizationStaffingCount>>
            GetPositionStaffingCountsAsync(
                CancellationToken cancellationToken = default)
            => Task.FromResult<
                IReadOnlyList<OrganizationStaffingCount>>([]);
    }

    [Fact]
    public async Task DeactivateDepartmentCommand_WhenConfirmed_CallsService()
    {
        Department department =
            CreateDepartment(
                "IT",
                "Công nghệ thông tin",
                true);

        var service =
            new StubDepartmentService(
                [department]);

        var dialogService =
            new StubDepartmentDialogService
            {
                ConfirmDeactivate = true
            };

        var viewModel =
            new DepartmentsViewModel(
                service,
                dialogService,
                new StubMembershipQueryService())
            {
                SelectedDepartment =
                    department
            };

        await viewModel
            .DeactivateDepartmentCommand
            .ExecuteAsync(null);

        Assert.Equal(
            department.Id,
            service.DeactivatedDepartmentId);
    }

    [Fact]
    public async Task ReactivateDepartmentCommand_WhenConfirmed_CallsService()
    {
        Department department =
            CreateDepartment(
                "OLD",
                "Phòng cũ",
                false);

        var service =
            new StubDepartmentService(
                [department]);

        var dialogService =
            new StubDepartmentDialogService
            {
                ConfirmReactivate = true
            };

        var viewModel =
            new DepartmentsViewModel(
                service,
                dialogService,
                new StubMembershipQueryService())
            {
                SelectedDepartment =
                    department
            };

        await viewModel
            .ReactivateDepartmentCommand
            .ExecuteAsync(null);

        Assert.Equal(
            department.Id,
            service.ReactivatedDepartmentId);
    }

    [Fact]
    public async Task AddDepartmentCommand_WhenServiceFails_ShowsErrorMessage()
    {
        var service =
            new StubDepartmentService(
                Array.Empty<Department>())
            {
                OperationResult =
                    new DepartmentOperationResult(
                        false,
                        "Mã phòng ban đã tồn tại.")
            };

        var dialogService =
            new StubDepartmentDialogService
            {
                AddResult =
                    new DepartmentEditorDialogResult(
                        "IT",
                        "Công nghệ thông tin")
            };

        var viewModel =
            new DepartmentsViewModel(
                service,
                dialogService,
                new StubMembershipQueryService());

        await viewModel
            .AddDepartmentCommand
            .ExecuteAsync(null);

        Assert.Equal(
            "Mã phòng ban đã tồn tại.",
            viewModel.ErrorMessage);
    }

    [Fact]
    public void ViewEmployeesCommand_WhenDepartmentSelected_ShowsEmployeesDialog()
    {
        Department department =
            CreateDepartment(
                "DEV",
                "Phát triển phần mềm");

        var service =
            new StubDepartmentService(
                [department]);

        var dialogService =
            new StubDepartmentDialogService();

        var viewModel =
            new DepartmentsViewModel(
                service,
                dialogService,
                new StubMembershipQueryService())
            {
                SelectedDepartment =
                    department
            };

        viewModel.ViewEmployeesCommand
            .Execute(null);

        Assert.Equal(
            department.Id,
            dialogService.ViewedDepartmentId);
    }

    [Fact]
    public async Task LoadAsync_WhenCalledAgain_RefreshesDepartmentStaffingCounts()
    {
        Department dev =
            CreateDepartment(
                "DEV",
                "Phát triển phần mềm");

        var service =
            new StubDepartmentService(
                [dev]);

        var dialogService =
            new StubDepartmentDialogService();

        var membershipService =
            new StubMembershipQueryService
            {
                DepartmentCounts =
                [
                    new OrganizationStaffingCount(
                    dev.Id,
                    ActiveCount: 2,
                    OnLeaveCount: 0,
                    InactiveCount: 1)
                ]
            };

        var viewModel =
            new DepartmentsViewModel(
                service,
                dialogService,
                membershipService);

        await viewModel.LoadAsync();

        DepartmentStaffingViewItem first =
            Assert.Single(
                viewModel.DepartmentRows);

        Assert.Equal(
            2,
            first.CurrentEmployeeCount);

        Assert.Equal(
            "2 hiện tại • 1 đã nghỉ",
            first.StaffingSummaryText);

        // Giả lập dữ liệu thay đổi trong màn Nhân viên:
        // thêm 1 active + rehire 1 inactive.
        membershipService.DepartmentCounts =
        [
            new OrganizationStaffingCount(
            dev.Id,
            ActiveCount: 4,
            OnLeaveCount: 0,
            InactiveCount: 0)
        ];

        await viewModel.LoadAsync();

        DepartmentStaffingViewItem refreshed =
            Assert.Single(
                viewModel.DepartmentRows);

        Assert.Equal(
            4,
            refreshed.CurrentEmployeeCount);

        Assert.Equal(
            0,
            refreshed.InactiveCount);

        Assert.Equal(
            "4 hiện tại • 0 đã nghỉ",
            refreshed.StaffingSummaryText);
    }
}
