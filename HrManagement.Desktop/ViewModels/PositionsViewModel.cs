using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Organization.Positions;
using HrManagement.Desktop.Services.Positions;
using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class PositionsViewModel
    : ObservableObject
{
    private readonly IPositionService
        _positionService;

    private readonly IPositionDialogService
        _positionDialogService;

    [ObservableProperty]
    private IReadOnlyList<Position>
        positions =
            Array.Empty<Position>();

    [ObservableProperty]
    private Position? selectedPosition;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    public IAsyncRelayCommand LoadCommand
    {
        get;
    }

    public IAsyncRelayCommand AddPositionCommand
    {
        get;
    }

    public IAsyncRelayCommand EditPositionCommand
    {
        get;
    }

    public IAsyncRelayCommand DeactivatePositionCommand
    {
        get;
    }

    public IAsyncRelayCommand ReactivatePositionCommand
    {
        get;
    }

    public PositionsViewModel(
        IPositionService positionService,
        IPositionDialogService positionDialogService)
    {
        _positionService =
            positionService;

        _positionDialogService =
            positionDialogService;

        LoadCommand =
            new AsyncRelayCommand(
                LoadAsync);

        AddPositionCommand =
            new AsyncRelayCommand(
                AddPositionAsync);

        EditPositionCommand =
            new AsyncRelayCommand(
                EditPositionAsync,
                CanEditPosition);

        DeactivatePositionCommand =
            new AsyncRelayCommand(
                DeactivatePositionAsync,
                CanDeactivatePosition);

        ReactivatePositionCommand =
            new AsyncRelayCommand(
                ReactivatePositionAsync,
                CanReactivatePosition);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Positions =
                await _positionService
                    .GetPositionsAsync();
        }
        catch (Exception)
        {
            Positions =
                Array.Empty<Position>();

            ErrorMessage =
                "Không thể tải danh sách chức danh.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AddPositionAsync()
    {
        PositionEditorDialogResult? dialogResult =
            _positionDialogService
                .ShowAddPositionDialog();

        if (dialogResult is null)
        {
            return;
        }

        ErrorMessage = null;

        PositionOperationResult result =
            await _positionService
                .CreatePositionAsync(
                    new CreatePositionRequest(
                        dialogResult.Code,
                        dialogResult.Name));

        if (!result.IsSuccessful)
        {
            ErrorMessage =
                result.ErrorMessage;

            return;
        }

        SelectedPosition = null;

        await LoadAsync();
    }

    private bool CanEditPosition()
    {
        return SelectedPosition is not null;
    }

    private async Task EditPositionAsync()
    {
        Position? position =
            SelectedPosition;

        if (position is null)
        {
            return;
        }

        PositionEditorDialogResult? dialogResult =
            _positionDialogService
                .ShowEditPositionDialog(
                    position);

        if (dialogResult is null)
        {
            return;
        }

        ErrorMessage = null;

        PositionOperationResult result =
            await _positionService
                .UpdatePositionAsync(
                    new UpdatePositionRequest(
                        position.Id,
                        dialogResult.Code,
                        dialogResult.Name));

        if (!result.IsSuccessful)
        {
            ErrorMessage =
                result.ErrorMessage;

            return;
        }

        SelectedPosition = null;

        await LoadAsync();
    }

    private bool CanDeactivatePosition()
    {
        return SelectedPosition?.IsActive == true;
    }

    private async Task DeactivatePositionAsync()
    {
        Position? position =
            SelectedPosition;

        if (position is null
            || !position.IsActive)
        {
            return;
        }

        bool confirmed =
            _positionDialogService
                .ConfirmDeactivatePosition(
                    position);

        if (!confirmed)
        {
            return;
        }

        ErrorMessage = null;

        PositionOperationResult result =
            await _positionService
                .DeactivatePositionAsync(
                    position.Id);

        if (!result.IsSuccessful)
        {
            ErrorMessage =
                result.ErrorMessage;

            return;
        }

        SelectedPosition = null;

        await LoadAsync();
    }

    private bool CanReactivatePosition()
    {
        return SelectedPosition?.IsActive == false;
    }

    private async Task ReactivatePositionAsync()
    {
        Position? position =
            SelectedPosition;

        if (position is null
            || position.IsActive)
        {
            return;
        }

        bool confirmed =
            _positionDialogService
                .ConfirmReactivatePosition(
                    position);

        if (!confirmed)
        {
            return;
        }

        ErrorMessage = null;

        PositionOperationResult result =
            await _positionService
                .ReactivatePositionAsync(
                    position.Id);

        if (!result.IsSuccessful)
        {
            ErrorMessage =
                result.ErrorMessage;

            return;
        }

        SelectedPosition = null;

        await LoadAsync();
    }

    partial void OnSelectedPositionChanged(
        Position? value)
    {
        EditPositionCommand
            .NotifyCanExecuteChanged();

        DeactivatePositionCommand
            .NotifyCanExecuteChanged();

        ReactivatePositionCommand
            .NotifyCanExecuteChanged();
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

    private sealed class StubPositionDialogService
    : IPositionDialogService
    {
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
}
