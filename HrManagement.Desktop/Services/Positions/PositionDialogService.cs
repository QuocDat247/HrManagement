using HrManagement.Desktop.Views;
using HrManagement.Domain.Organization.Positions;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace HrManagement.Desktop.Services.Positions;

public sealed class PositionDialogService
    : IPositionDialogService
{
    private readonly IServiceProvider
        _serviceProvider;

    public PositionDialogService(
        IServiceProvider serviceProvider)
    {
        _serviceProvider =
            serviceProvider;
    }

    public PositionEditorDialogResult?
        ShowAddPositionDialog()
    {
        PositionEditorWindow window =
            CreateEditorWindow();

        window.LoadForAdd();

        bool? result =
            window.ShowDialog();

        return result == true
            ? window.Result
            : null;
    }

    public void ShowEmployees(
    Position position)
    {
        ArgumentNullException.ThrowIfNull(
            position);

        OrganizationEmployeesWindow window =
            _serviceProvider
                .GetRequiredService<
                    OrganizationEmployeesWindow>();

        window.Owner =
            System.Windows.Application
                .Current
                .MainWindow;

        window.LoadPosition(
            position);

        window.ShowDialog();
    }

    public PositionEditorDialogResult?
        ShowEditPositionDialog(
            Position position)
    {
        ArgumentNullException.ThrowIfNull(
            position);

        PositionEditorWindow window =
            CreateEditorWindow();

        window.LoadForEdit(
            position);

        bool? result =
            window.ShowDialog();

        return result == true
            ? window.Result
            : null;
    }

    public bool ConfirmDeactivatePosition(
        Position position)
    {
        ArgumentNullException.ThrowIfNull(
            position);

        MessageBoxResult result =
            MessageBox.Show(
                $"Ngừng sử dụng chức danh \"{position.Name}\"?\n\n"
                + "Chức danh vẫn được giữ lại trong lịch sử nhưng sẽ không còn dùng cho các lựa chọn mới.",
                "Ngừng sử dụng chức danh",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

        return result ==
            MessageBoxResult.Yes;
    }

    public bool ConfirmReactivatePosition(
        Position position)
    {
        ArgumentNullException.ThrowIfNull(
            position);

        MessageBoxResult result =
            MessageBox.Show(
                $"Kích hoạt lại chức danh \"{position.Name}\"?",
                "Kích hoạt chức danh",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

        return result ==
            MessageBoxResult.Yes;
    }

    private PositionEditorWindow
        CreateEditorWindow()
    {
        PositionEditorWindow window =
            _serviceProvider
                .GetRequiredService<
                    PositionEditorWindow>();

        window.Owner =
            System.Windows.Application
                .Current
                .MainWindow;

        return window;
    }
}
