using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Desktop.Services.Positions;

public interface IPositionDialogService
{
    PositionEditorDialogResult?
        ShowAddPositionDialog();

    PositionEditorDialogResult?
        ShowEditPositionDialog(
            Position position);

    bool ConfirmDeactivatePosition(
        Position position);

    bool ConfirmReactivatePosition(
        Position position);
}
