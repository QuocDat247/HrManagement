using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class PositionEditorViewModel
    : ObservableObject
{
    [ObservableProperty]
    private string title =
        "Thêm chức danh";

    [ObservableProperty]
    private string code =
        string.Empty;

    [ObservableProperty]
    private string name =
        string.Empty;

    [ObservableProperty]
    private string? errorMessage;

    public event EventHandler?
        ConfirmSucceeded;

    public IRelayCommand ConfirmCommand
    {
        get;
    }

    public PositionEditorViewModel()
    {
        ConfirmCommand =
            new RelayCommand(
                Confirm);
    }

    public void LoadForAdd()
    {
        Title =
            "Thêm chức danh";

        Code =
            string.Empty;

        Name =
            string.Empty;

        ErrorMessage =
            null;
    }

    public void LoadForEdit(
        Position position)
    {
        ArgumentNullException.ThrowIfNull(
            position);

        Title =
            "Sửa chức danh";

        Code =
            position.Code;

        Name =
            position.Name;

        ErrorMessage =
            null;
    }

    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(
                Code))
        {
            ErrorMessage =
                "Vui lòng nhập mã chức danh.";

            return;
        }

        if (string.IsNullOrWhiteSpace(
                Name))
        {
            ErrorMessage =
                "Vui lòng nhập tên chức danh.";

            return;
        }

        Code =
            Code.Trim()
                .ToUpperInvariant();

        Name =
            Name.Trim();

        ErrorMessage =
            null;

        ConfirmSucceeded?.Invoke(
            this,
            EventArgs.Empty);
    }
}
