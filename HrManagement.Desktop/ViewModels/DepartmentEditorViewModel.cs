using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Domain.Organization.Departments;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class DepartmentEditorViewModel
    : ObservableObject
{
    [ObservableProperty]
    private string title =
        "Thêm phòng ban";

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

    public DepartmentEditorViewModel()
    {
        ConfirmCommand =
            new RelayCommand(
                Confirm);
    }

    public void LoadForAdd()
    {
        Title =
            "Thêm phòng ban";

        Code =
            string.Empty;

        Name =
            string.Empty;

        ErrorMessage =
            null;
    }

    public void LoadForEdit(
        Department department)
    {
        ArgumentNullException.ThrowIfNull(
            department);

        Title =
            "Sửa phòng ban";

        Code =
            department.Code;

        Name =
            department.Name;

        ErrorMessage =
            null;
    }

    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(
                Code))
        {
            ErrorMessage =
                "Vui lòng nhập mã phòng ban.";

            return;
        }

        if (string.IsNullOrWhiteSpace(
                Name))
        {
            ErrorMessage =
                "Vui lòng nhập tên phòng ban.";

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
