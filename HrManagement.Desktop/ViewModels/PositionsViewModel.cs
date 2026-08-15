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

    public IRelayCommand ViewEmployeesCommand
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

        ViewEmployeesCommand =
            new RelayCommand(
                ViewEmployees,
                CanViewEmployees);

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

    private bool CanViewEmployees()
    {
        return SelectedPosition is not null;
    }

    private void ViewEmployees()
    {
        Position? position =
            SelectedPosition;

        if (position is null)
        {
            return;
        }

        _positionDialogService
            .ShowEmployees(
                position);
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

        ViewEmployeesCommand
            .NotifyCanExecuteChanged();
    }
}
