using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HrManagement.Application.Organization.Positions;
using HrManagement.Desktop.Services.Positions;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Organization.Positions;
using HrManagement.Application.Organization.Memberships;

namespace HrManagement.Tests.ViewModels;
public sealed class PositionsViewModelTests
{
    [Fact]
    public async Task DeactivatePositionCommand_WhenConfirmed_CallsService()
    {
        Position position =
            CreatePosition(
                "DEV",
                "Lập trình viên",
                true);

        var service =
            new StubPositionService(
                [position]);

        var dialogService =
            new StubPositionDialogService
            {
                ConfirmDeactivate = true
            };

        var viewModel =
            new PositionsViewModel(
                service,
                dialogService,
                new StubMembershipQueryService())
            {
                SelectedPosition =
                    position
            };

        await viewModel
            .DeactivatePositionCommand
            .ExecuteAsync(null);

        Assert.Equal(
            position.Id,
            service.DeactivatedPositionId);
    }

    [Fact]
    public async Task ReactivatePositionCommand_WhenConfirmed_CallsService()
    {
        Position position =
            CreatePosition(
                "OLD",
                "Chức danh cũ",
                false);

        var service =
            new StubPositionService(
                [position]);

        var dialogService =
            new StubPositionDialogService
            {
                ConfirmReactivate = true
            };

        var viewModel =
            new PositionsViewModel(
                service,
                dialogService,
                new StubMembershipQueryService())
            {
                SelectedPosition =
                    position
            };

        await viewModel
            .ReactivatePositionCommand
            .ExecuteAsync(null);

        Assert.Equal(
            position.Id,
            service.ReactivatedPositionId);
    }

    [Fact]
    public async Task AddPositionCommand_WhenServiceFails_ShowsErrorMessage()
    {
        var service =
            new StubPositionService(
                Array.Empty<Position>())
            {
                OperationResult =
                    new PositionOperationResult(
                        false,
                        "Mã chức danh đã tồn tại.")
            };

        var dialogService =
            new StubPositionDialogService
            {
                AddResult =
                    new PositionEditorDialogResult(
                        "DEV",
                        "Lập trình viên")
            };

        var viewModel =
            new PositionsViewModel(
                service,
                dialogService,
                new StubMembershipQueryService());

        await viewModel
            .AddPositionCommand
            .ExecuteAsync(null);

        Assert.Equal(
            "Mã chức danh đã tồn tại.",
            viewModel.ErrorMessage);
    }

    private static Position CreatePosition(
    string code,
    string name,
    bool isActive = true)
    {
        return new Position(
            Guid.NewGuid(),
            code,
            name,
            isActive);
    }

    [Fact]
    public async Task LoadAsync_MapsPositionStaffingCounts()
    {
        Position developer =
            CreatePosition(
                "DEV",
                "Lập trình viên");

        Position qa =
            CreatePosition(
                "QA",
                "Kiểm thử");

        var service =
            new StubPositionService(
                [developer, qa]);

        var dialogService =
            new StubPositionDialogService();

        var membershipService =
            new StubMembershipQueryService
            {
                PositionCounts =
                [
                    new OrganizationStaffingCount(
                    developer.Id,
                    ActiveCount: 4,
                    OnLeaveCount: 2,
                    InactiveCount: 1)
                ]
            };

        var viewModel =
            new PositionsViewModel(
                service,
                dialogService,
                membershipService);

        await viewModel.LoadAsync();

        PositionStaffingViewItem developerRow =
            Assert.Single(
                viewModel.PositionRows.Where(
                    row =>
                        row.Id == developer.Id));

        Assert.Equal(
            6,
            developerRow.CurrentEmployeeCount);

        Assert.Equal(
            7,
            developerRow.TotalLinkedEmployeeCount);

        PositionStaffingViewItem qaRow =
            Assert.Single(
                viewModel.PositionRows.Where(
                    row =>
                        row.Id == qa.Id));

        Assert.Equal(
            0,
            qaRow.CurrentEmployeeCount);

        viewModel.SelectedPositionRow =
            developerRow;

        Assert.Same(
            developer,
            viewModel.SelectedPosition);

        Assert.True(
            viewModel.ViewEmployeesCommand
                .CanExecute(null));

        Assert.Equal(
            "6 hiện tại • 1 đã nghỉ",
            developerRow.StaffingSummaryText);

        Assert.Equal(
            "0 hiện tại • 0 đã nghỉ",
            qaRow.StaffingSummaryText);

        Assert.Equal(
            "Đang làm việc: 4 • Nghỉ phép: 2 • Đã nghỉ: 1",
            developerRow.StaffingDetailText);
    }

    [Fact]
    public async Task LoadAsync_LoadsPositions()
    {
        var positions =
            new[]
            {
            CreatePosition(
                "DEV",
                "Lập trình viên"),

            CreatePosition(
                "MGR",
                "Trưởng phòng")
            };

        var service =
            new StubPositionService(
                positions);

        var dialogService =
            new StubPositionDialogService();

        var viewModel =
            new PositionsViewModel(
                service,
                dialogService,
                new StubMembershipQueryService());

        await viewModel.LoadAsync();

        Assert.Equal(
            2,
            viewModel.Positions.Count);

        Assert.Null(
            viewModel.ErrorMessage);

        Assert.False(
            viewModel.IsLoading);
    }

    [Fact]
    public async Task AddPositionCommand_WhenConfirmed_CallsService()
    {
        var service =
            new StubPositionService(
                Array.Empty<Position>());

        var dialogService =
            new StubPositionDialogService
            {
                AddResult =
                    new PositionEditorDialogResult(
                        "DEV",
                        "Lập trình viên")
            };

        var viewModel =
            new PositionsViewModel(
                service,
                dialogService,
                new StubMembershipQueryService());

        await viewModel
            .AddPositionCommand
            .ExecuteAsync(null);

        Assert.NotNull(
            service.CreateRequest);

        Assert.Equal(
            "DEV",
            service.CreateRequest!.Code);

        Assert.Equal(
            "Lập trình viên",
            service.CreateRequest.Name);
    }

    [Fact]
    public async Task EditPositionCommand_WhenConfirmed_CallsServiceForSelectedPosition()
    {
        Position position =
            CreatePosition(
                "DEV",
                "Lập trình viên");

        var service =
            new StubPositionService(
                [position]);

        var dialogService =
            new StubPositionDialogService
            {
                EditResult =
                    new PositionEditorDialogResult(
                        "SWE",
                        "Kỹ sư phần mềm")
            };

        var viewModel =
            new PositionsViewModel(
                service,
                dialogService,
                new StubMembershipQueryService())
            {
                SelectedPosition =
                    position
            };

        await viewModel
            .EditPositionCommand
            .ExecuteAsync(null);

        Assert.NotNull(
            service.UpdateRequest);

        Assert.Equal(
            position.Id,
            service.UpdateRequest!.PositionId);

        Assert.Equal(
            "SWE",
            service.UpdateRequest.Code);

        Assert.Equal(
            "Kỹ sư phần mềm",
            service.UpdateRequest.Name);
    }

    [Fact]
    public void DeactivatePositionCommand_CanExecuteOnlyForActivePosition()
    {
        var service =
            new StubPositionService(
                Array.Empty<Position>());

        var dialogService =
            new StubPositionDialogService();

        var viewModel =
            new PositionsViewModel(
                service,
                dialogService,
                new StubMembershipQueryService());

        Assert.False(
            viewModel
                .DeactivatePositionCommand
                .CanExecute(null));

        viewModel.SelectedPosition =
            CreatePosition(
                "DEV",
                "Lập trình viên",
                true);

        Assert.True(
            viewModel
                .DeactivatePositionCommand
                .CanExecute(null));

        viewModel.SelectedPosition =
            CreatePosition(
                "OLD",
                "Chức danh cũ",
                false);

        Assert.False(
            viewModel
                .DeactivatePositionCommand
                .CanExecute(null));
    }

    [Fact]
    public void ReactivatePositionCommand_CanExecuteOnlyForInactivePosition()
    {
        var service =
            new StubPositionService(
                Array.Empty<Position>());

        var dialogService =
            new StubPositionDialogService();

        var viewModel =
            new PositionsViewModel(
                service,
                dialogService,
                new StubMembershipQueryService());

        Assert.False(
            viewModel
                .ReactivatePositionCommand
                .CanExecute(null));

        viewModel.SelectedPosition =
            CreatePosition(
                "DEV",
                "Lập trình viên",
                true);

        Assert.False(
            viewModel
                .ReactivatePositionCommand
                .CanExecute(null));

        viewModel.SelectedPosition =
            CreatePosition(
                "OLD",
                "Chức danh cũ",
                false);

        Assert.True(
            viewModel
                .ReactivatePositionCommand
                .CanExecute(null));
    }

    private sealed class StubPositionDialogService
    : IPositionDialogService
    {
        public Guid? ViewedPositionId
        {
            get;
            private set;
        }

        public void ShowEmployees(
            Position position)
        {
            ViewedPositionId =
                position.Id;
        }

        public PositionEditorDialogResult?
            AddResult
        {
            get;
            set;
        }

        public PositionEditorDialogResult?
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

        public PositionEditorDialogResult?
            ShowAddPositionDialog()
        {
            return AddResult;
        }

        public PositionEditorDialogResult?
            ShowEditPositionDialog(
                Position position)
        {
            return EditResult;
        }

        public bool ConfirmDeactivatePosition(
            Position position)
        {
            return ConfirmDeactivate;
        }

        public bool ConfirmReactivatePosition(
            Position position)
        {
            return ConfirmReactivate;
        }
    }

    private sealed class StubPositionService
    : IPositionService
    {
        private readonly IReadOnlyList<Position>
            _positions;

        public CreatePositionRequest?
            CreateRequest
        {
            get;
            private set;
        }

        public UpdatePositionRequest?
            UpdateRequest
        {
            get;
            private set;
        }

        public Guid?
            DeactivatedPositionId
        {
            get;
            private set;
        }

        public Guid?
            ReactivatedPositionId
        {
            get;
            private set;
        }

        public PositionOperationResult
            OperationResult
        {
            get;
            set;
        } =
            new(
                true,
                null);

        public StubPositionService(
            IReadOnlyList<Position> positions)
        {
            _positions =
                positions;
        }

        public Task<IReadOnlyList<Position>>
            GetPositionsAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _positions);
        }

        public Task<PositionOperationResult>
            CreatePositionAsync(
                CreatePositionRequest request,
                CancellationToken cancellationToken = default)
        {
            CreateRequest =
                request;

            return Task.FromResult(
                OperationResult);
        }

        public Task<PositionOperationResult>
            UpdatePositionAsync(
                UpdatePositionRequest request,
                CancellationToken cancellationToken = default)
        {
            UpdateRequest =
                request;

            return Task.FromResult(
                OperationResult);
        }

        public Task<PositionOperationResult>
            DeactivatePositionAsync(
                Guid positionId,
                CancellationToken cancellationToken = default)
        {
            DeactivatedPositionId =
                positionId;

            return Task.FromResult(
                OperationResult);
        }

        public Task<PositionOperationResult>
            ReactivatePositionAsync(
                Guid positionId,
                CancellationToken cancellationToken = default)
        {
            ReactivatedPositionId =
                positionId;

            return Task.FromResult(
                OperationResult);
        }
    }

    private sealed class StubMembershipQueryService
    : IOrganizationMembershipQueryService
    {
        public IReadOnlyList<OrganizationStaffingCount>
            PositionCounts
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
            => Task.FromResult<
                IReadOnlyList<OrganizationStaffingCount>>([]);

        public Task<IReadOnlyList<OrganizationStaffingCount>>
            GetPositionStaffingCountsAsync(
                CancellationToken cancellationToken = default)
            => Task.FromResult(PositionCounts);
    }

    [Fact]
    public void ViewEmployeesCommand_WhenPositionSelected_ShowsEmployeesDialog()
    {
        Position position =
            CreatePosition(
                "DEV",
                "Lập trình viên");

        var service =
            new StubPositionService(
                [position]);

        var dialogService =
            new StubPositionDialogService();

        var viewModel =
            new PositionsViewModel(
                service,
                dialogService,
                new StubMembershipQueryService())
            {
                SelectedPosition =
                    position
            };

        viewModel.ViewEmployeesCommand
            .Execute(null);

        Assert.Equal(
            position.Id,
            dialogService.ViewedPositionId);
    }

    [Fact]
    public async Task LoadAsync_WhenCalledAgain_RefreshesPositionStaffingCounts()
    {
        Position developer =
            CreatePosition(
                "DEV",
                "Lập trình viên");

        var service =
            new StubPositionService(
                [developer]);

        var dialogService =
            new StubPositionDialogService();

        var membershipService =
            new StubMembershipQueryService
            {
                PositionCounts =
                [
                    new OrganizationStaffingCount(
                    developer.Id,
                    ActiveCount: 3,
                    OnLeaveCount: 1,
                    InactiveCount: 2)
                ]
            };

        var viewModel =
            new PositionsViewModel(
                service,
                dialogService,
                membershipService);

        await viewModel.LoadAsync();

        PositionStaffingViewItem first =
            Assert.Single(
                viewModel.PositionRows);

        Assert.Equal(
            4,
            first.CurrentEmployeeCount);

        Assert.Equal(
            "4 hiện tại • 2 đã nghỉ",
            first.StaffingSummaryText);

        // Giả lập employee đổi/re-hire/deactivate
        // rồi quay lại màn Chức danh.
        membershipService.PositionCounts =
        [
            new OrganizationStaffingCount(
            developer.Id,
            ActiveCount: 5,
            OnLeaveCount: 0,
            InactiveCount: 1)
        ];

        await viewModel.LoadAsync();

        PositionStaffingViewItem refreshed =
            Assert.Single(
                viewModel.PositionRows);

        Assert.Equal(
            5,
            refreshed.CurrentEmployeeCount);

        Assert.Equal(
            1,
            refreshed.InactiveCount);

        Assert.Equal(
            "5 hiện tại • 1 đã nghỉ",
            refreshed.StaffingSummaryText);
    }
}
