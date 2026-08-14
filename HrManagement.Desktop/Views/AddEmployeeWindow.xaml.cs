using System.Windows;
using HrManagement.Desktop.ViewModels;

namespace HrManagement.Desktop.Views;

public partial class AddEmployeeWindow : Window
{
    private readonly AddEmployeeViewModel _viewModel;

    public AddEmployeeWindow(AddEmployeeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // Giữ nguyên dòng đăng ký sự kiện SaveSucceeded cũ của bạn:
        _viewModel.SaveSucceeded += ViewModel_SaveSucceeded;

        // THÊM DÒNG MỚI NÀY:
        Loaded += AddEmployeeWindow_Loaded;
    }

    private async void AddEmployeeWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadOrganizationOptionsAsync();
    }

    private void ViewModel_SaveSucceeded(
        object? sender,
        EventArgs e)
    {
        DialogResult = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.SaveSucceeded -= ViewModel_SaveSucceeded;

        base.OnClosed(e);

        Loaded -= AddEmployeeWindow_Loaded;
    }
}
