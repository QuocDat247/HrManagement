using System;
using HrManagement.Application.Attendance.Schedules;
using HrManagement.Application.Workspaces.WorkSchedules;
using HrManagement.Desktop.Services;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Tests.Attendance;

public sealed class WorkScheduleWorkspaceTemplateActionsTests
{
    [Fact]
    public async Task LoadAsync_SelectedScheduleLoadsEditor()
    {
        TemplateTestContext context =
            CreateContext();

        await context.ViewModel.LoadAsync();

        Assert.Equal(
            "OFFICE",
            context.ViewModel.ScheduleEditorCode);

        Assert.Equal(
            "Giờ hành chính",
            context.ViewModel.ScheduleEditorName);

        Assert.Equal(
            "SE Asia Standard Time",
            context.ViewModel.ScheduleEditorTimeZoneId);

        Assert.False(
            context.ViewModel.IsCreatingSchedule);
    }

    [Fact]
    public async Task NewScheduleCommand_StartsCleanCreateMode()
    {
        TemplateTestContext context =
            CreateContext();

        await context.ViewModel.LoadAsync();

        context.ViewModel
            .NewScheduleCommand
            .Execute(
                null);

        Assert.True(
            context.ViewModel.IsCreatingSchedule);

        Assert.Null(
            context.ViewModel.SelectedScheduleItem);

        Assert.Null(
            context.ViewModel.ScheduleEditorCode);

        Assert.Null(
            context.ViewModel.ScheduleEditorName);

        Assert.Equal(
            "SE Asia Standard Time",
            context.ViewModel.ScheduleEditorTimeZoneId);
    }

    [Fact]
    public async Task SaveScheduleCommand_CreateMapsRequest()
    {
        TemplateTestContext context =
            CreateContext();

        await context.ViewModel.LoadAsync();

        context.ViewModel
            .NewScheduleCommand
            .Execute(
                null);

        context.ViewModel.ScheduleEditorCode =
            "NIGHT";

        context.ViewModel.ScheduleEditorName =
            "Ca đêm";

        context.ViewModel.ScheduleEditorTimeZoneId =
            "SE Asia Standard Time";

        await context.ViewModel
            .SaveScheduleCommand
            .ExecuteAsync(
                null);

        CreateWorkScheduleRequest request =
            Assert.IsType<CreateWorkScheduleRequest>(
                context.ManagementService.LastCreateRequest);

        Assert.Equal(
            "NIGHT",
            request.Code);

        Assert.Equal(
            "Ca đêm",
            request.Name);

        Assert.Contains(
            "Đã tạo mẫu lịch NIGHT - Ca đêm",
            context.ViewModel.OperationMessage);

        Assert.Null(
            context.ViewModel.ErrorMessage);
    }

    [Fact]
    public async Task SaveScheduleCommand_UpdateMapsSelectedSchedule()
    {
        TemplateTestContext context =
            CreateContext();

        await context.ViewModel.LoadAsync();

        context.ViewModel.ScheduleEditorName =
            "Giờ hành chính mới";

        await context.ViewModel
            .SaveScheduleCommand
            .ExecuteAsync(
                null);

        UpdateWorkScheduleRequest request =
            Assert.IsType<UpdateWorkScheduleRequest>(
                context.ManagementService.LastUpdateRequest);

        Assert.Equal(
            context.ScheduleId,
            request.WorkScheduleId);

        Assert.Equal(
            "OFFICE",
            request.Code);

        Assert.Equal(
            "Giờ hành chính mới",
            request.Name);
    }

    [Fact]
    public async Task DeactivateScheduleCommand_MapsSelectedSchedule()
    {
        TemplateTestContext context =
            CreateContext();

        await context.ViewModel.LoadAsync();

        Assert.True(
            context.ViewModel
                .DeactivateScheduleCommand
                .CanExecute(
                    null));

        await context.ViewModel
            .DeactivateScheduleCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            context.ScheduleId,
            context.ManagementService.LastDeactivateId);

        Assert.Contains(
            "Đã ngừng sử dụng mẫu lịch",
            context.ViewModel.OperationMessage);
    }

    [Fact]
    public async Task ReactivateScheduleCommand_ServiceFailureShowsBusinessError()
    {
        TemplateTestContext context =
            CreateContext(
                isActive: false);

        context.ManagementService.ReactivateResult =
            new WorkScheduleManagementResult(
                false,
                null,
                "Lịch làm việc phải có ít nhất một ngày làm việc trước khi kích hoạt.");

        await context.ViewModel.LoadAsync();

        Assert.True(
            context.ViewModel
                .ReactivateScheduleCommand
                .CanExecute(
                    null));

        await context.ViewModel
            .ReactivateScheduleCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            "Lịch làm việc phải có ít nhất một ngày làm việc trước khi kích hoạt.",
            context.ViewModel.ErrorMessage);

        Assert.Null(
            context.ViewModel.OperationMessage);
    }

    [Fact]
    public async Task CloneScheduleCommand_RequiresSelectedSchedule()
    {
        TemplateTestContext context =
            CreateContext();

        Assert.False(
            context.ViewModel
                .CloneScheduleCommand
                .CanExecute(
                    null));

        await context.ViewModel.LoadAsync();

        Assert.True(
            context.ViewModel
                .CloneScheduleCommand
                .CanExecute(
                    null));
    }

    [Fact]
    public async Task CloneScheduleCommand_PrefillsEditorFromSource()
    {
        TemplateTestContext context =
            CreateContext();

        await context.ViewModel.LoadAsync();

        context.ViewModel
            .CloneScheduleCommand
            .Execute(
                null);

        Assert.True(
            context.ViewModel.IsCreatingSchedule);

        Assert.True(
            context.ViewModel.IsCloningSchedule);

        Assert.Null(
            context.ViewModel.SelectedScheduleItem);

        Assert.Equal(
            "OFFICE-COPY",
            context.ViewModel.ScheduleEditorCode);

        Assert.Equal(
            "Bản sao - Giờ hành chính",
            context.ViewModel.ScheduleEditorName);

        Assert.Equal(
            "SE Asia Standard Time",
            context.ViewModel.ScheduleEditorTimeZoneId);

        Assert.False(
            context.ViewModel.IsScheduleTimeZoneEditorEnabled);
    }

    [Fact]
    public async Task SaveScheduleCommand_CloneMapsSourceAndNewIdentity()
    {
        TemplateTestContext context =
            CreateContext();

        await context.ViewModel.LoadAsync();

        context.ViewModel
            .CloneScheduleCommand
            .Execute(
                null);

        context.ViewModel.ScheduleEditorCode =
            "OFFICE-ALT";

        context.ViewModel.ScheduleEditorName =
            "Ca hành chính thay thế";

        await context.ViewModel
            .SaveScheduleCommand
            .ExecuteAsync(
                null);

        CloneWorkScheduleRequest request =
            Assert.IsType<CloneWorkScheduleRequest>(
                context.ManagementService.LastCloneRequest);

        Assert.Equal(
            context.ScheduleId,
            request.SourceWorkScheduleId);

        Assert.Equal(
            "OFFICE-ALT",
            request.Code);

        Assert.Equal(
            "Ca hành chính thay thế",
            request.Name);

        Assert.Contains(
            "Đã sao chép thành mẫu lịch OFFICE-ALT",
            context.ViewModel.OperationMessage);

        Assert.False(
            context.ViewModel.IsCloningSchedule);
    }

    [Fact]
    public async Task NewScheduleCommand_LeavesCloneMode()
    {
        TemplateTestContext context =
            CreateContext();

        await context.ViewModel.LoadAsync();

        context.ViewModel
            .CloneScheduleCommand
            .Execute(
                null);

        context.ViewModel
            .NewScheduleCommand
            .Execute(
                null);

        Assert.True(
            context.ViewModel.IsCreatingSchedule);

        Assert.False(
            context.ViewModel.IsCloningSchedule);

        Assert.True(
            context.ViewModel.IsScheduleTimeZoneEditorEnabled);

        Assert.Null(
            context.ViewModel.ScheduleEditorCode);

        Assert.Null(
            context.ViewModel.ScheduleEditorName);
    }

    [Fact]
    public async Task DeleteScheduleCommand_RequiresInactiveSchedule()
    {
        TemplateTestContext activeContext =
            CreateContext(
                isActive: true);

        await activeContext.ViewModel.LoadAsync();

        Assert.False(
            activeContext.ViewModel
                .DeleteScheduleCommand
                .CanExecute(
                    null));

        TemplateTestContext inactiveContext =
            CreateContext(
                isActive: false);

        await inactiveContext.ViewModel.LoadAsync();

        Assert.True(
            inactiveContext.ViewModel
                .DeleteScheduleCommand
                .CanExecute(
                    null));
    }

    [Fact]
    public async Task DeleteScheduleCommand_CancelDoesNotDelete()
    {
        TemplateTestContext context =
            CreateContext(
                isActive: false);

        context.ConfirmationService.Result =
            false;

        await context.ViewModel.LoadAsync();

        await context.ViewModel
            .DeleteScheduleCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            1,
            context.ConfirmationService.ConfirmCallCount);

        Assert.Null(
            context.ManagementService
                .LastDeletedWorkScheduleId);
    }

    [Fact]
    public async Task DeleteScheduleCommand_ConfirmedDeletesSelectedSchedule()
    {
        TemplateTestContext context =
            CreateContext(
                isActive: false);

        await context.ViewModel.LoadAsync();

        await context.ViewModel
            .DeleteScheduleCommand
            .ExecuteAsync(
                null);

        Assert.Equal(
            context.ScheduleId,
            context.ManagementService
                .LastDeletedWorkScheduleId);

        Assert.Contains(
            "Đã xóa mẫu lịch",
            context.ViewModel.OperationMessage);
    }

    [Fact]
    public async Task DeleteScheduleCommand_BusinessFailureShowsError()
    {
        TemplateTestContext context =
            CreateContext(
                isActive: false);

        context.ManagementService.DeleteResult =
            new WorkScheduleManagementResult(
                false,
                ErrorMessage:
                    "Mẫu lịch đã có lịch sử sử dụng. "
                    + "Hãy ngừng sử dụng thay vì xóa.");

        await context.ViewModel.LoadAsync();

        await context.ViewModel
            .DeleteScheduleCommand
            .ExecuteAsync(
                null);

        Assert.Contains(
            "đã có lịch sử sử dụng",
            context.ViewModel.ErrorMessage);

        Assert.Null(
            context.ViewModel.OperationMessage);
    }
    private static TemplateTestContext CreateContext(
        bool isActive = true)
    {
        Guid scheduleId =
            Guid.NewGuid();

        var queryService =
            new TestQueryService
            {
                Snapshot =
                    new WorkScheduleWorkspaceSnapshot(
                        [
                            new WorkScheduleWorkspaceScheduleItem(
                                scheduleId,
                                "OFFICE",
                                "Giờ hành chính",
                                "SE Asia Standard Time",
                                isActive)
                        ],
                        [],
                        [])
            };

        var confirmationService =
            new TestConfirmationService();

        var managementService =
            new TestManagementService();

        var viewModel =
            new WorkScheduleWorkspaceViewModel(
                queryService,
                new TestAssignmentService(),
                managementService,
                new TestWorkScheduleDayManagementService(),
                confirmationService,
                TimeProvider.System);

        return new TemplateTestContext(
            viewModel,
            managementService,
            confirmationService,
            scheduleId);
    }

    private sealed record TestContext(
        WorkScheduleWorkspaceViewModel ViewModel,
        TestManagementService ManagementService,
        Guid ScheduleId);

    private sealed class TestQueryService
        : IWorkScheduleWorkspaceQueryService
    {
        public WorkScheduleWorkspaceSnapshot Snapshot
        {
            get;
            init;
        } =
            new(
                [],
                [],
                []);

        public Task<IReadOnlyList<WorkScheduleWorkspaceEmployeeItem>>
            GetEmployeesAsync(
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                IReadOnlyList<WorkScheduleWorkspaceEmployeeItem>>(
                    []);
        }

        public Task<WorkScheduleWorkspaceSnapshot> GetAsync(
            WorkScheduleWorkspaceQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Snapshot);
        }
    }

    private sealed class TemplateTestContext
    {
        public WorkScheduleWorkspaceViewModel ViewModel
        {
            get;
        }

        public TestManagementService ManagementService
        {
            get;
        }

        public TestConfirmationService ConfirmationService
        {
            get;
        }

        public Guid ScheduleId
        {
            get;
        }

        public TemplateTestContext(
            WorkScheduleWorkspaceViewModel viewModel,
            TestManagementService managementService,
            TestConfirmationService confirmationService,
            Guid scheduleId)
        {
            ViewModel =
                viewModel;

            ManagementService =
                managementService;

            ConfirmationService =
                confirmationService;

            ScheduleId =
                scheduleId;
        }
    }

    private sealed class TestAssignmentService
        : IEmployeeWorkScheduleAssignmentService
    {
        public Task<AssignEmployeeWorkScheduleResult> AssignAsync(
            AssignEmployeeWorkScheduleRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new AssignEmployeeWorkScheduleResult(
                    true));
        }
    }

    private sealed class TestManagementService
        : IWorkScheduleManagementService
    {
        public Task<WorkScheduleManagementResult> DeleteAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken = default)
        {
            LastDeletedWorkScheduleId =
                workScheduleId;

            WorkScheduleManagementResult result =
                DeleteResult.IsSuccessful
                    ? new WorkScheduleManagementResult(
                        true,
                        workScheduleId)
                    : DeleteResult;

            return Task.FromResult(
                result);
        }

        public Task<WorkScheduleManagementResult> CloneAsync(
            CloneWorkScheduleRequest request,
            CancellationToken cancellationToken = default)
        {
            LastCloneRequest =
                request;

            return Task.FromResult(
                new WorkScheduleManagementResult(
                    true,
                    Guid.NewGuid()));
        }

        public Guid? LastDeletedWorkScheduleId
        {
            get;
            private set;
        }

        public WorkScheduleManagementResult DeleteResult
        {
            get;
            set;
        } =
                new(
                    true);

        public CloneWorkScheduleRequest?
            LastCloneRequest
        {
            get;
            private set;
        }

        public CreateWorkScheduleRequest?
            LastCreateRequest
        {
            get;
            private set;
        }

        public UpdateWorkScheduleRequest?
            LastUpdateRequest
        {
            get;
            private set;
        }

        public Guid? LastDeactivateId
        {
            get;
            private set;
        }

        public WorkScheduleManagementResult ReactivateResult
        {
            get;
            set;
        } =
            new(
                true,
                Guid.NewGuid());

        public Task<WorkScheduleManagementResult> CreateAsync(
            CreateWorkScheduleRequest request,
            CancellationToken cancellationToken = default)
        {
            LastCreateRequest =
                request;

            return Task.FromResult(
                new WorkScheduleManagementResult(
                    true,
                    Guid.NewGuid()));
        }

        public Task<WorkScheduleManagementResult> UpdateAsync(
            UpdateWorkScheduleRequest request,
            CancellationToken cancellationToken = default)
        {
            LastUpdateRequest =
                request;

            return Task.FromResult(
                new WorkScheduleManagementResult(
                    true,
                    request.WorkScheduleId));
        }

        public Task<WorkScheduleManagementResult> DeactivateAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken = default)
        {
            LastDeactivateId =
                workScheduleId;

            return Task.FromResult(
                new WorkScheduleManagementResult(
                    true,
                    workScheduleId));
        }

        public Task<WorkScheduleManagementResult> ReactivateAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                ReactivateResult);
        }
    }

    private sealed class TestWorkScheduleDayManagementService
    : IWorkScheduleDayManagementService
    {
        public Task<WorkScheduleDayManagementResult> UpdateAsync(
            UpdateWorkScheduleDayRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                new WorkScheduleDayManagementResult(
                    true));
    }

    private sealed class AlwaysConfirmService
    : IUserConfirmationService
    {
        public bool Confirm(
            string title,
            string message)
        {
            return true;
        }
    }

    private sealed class TestConfirmationService
    : IUserConfirmationService
    {
        public bool Result
        {
            get;
            set;
        } =
            true;

        public int ConfirmCallCount
        {
            get;
            private set;
        }

        public bool Confirm(
            string title,
            string message)
        {
            ConfirmCallCount++;

            return Result;
        }
    }
}
