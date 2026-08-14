using System.Windows;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.Views;

public partial class EditEmployeeWindow : Window
{
    private readonly EditEmployeeViewModel _viewModel;

    public EditEmployeeWindow(EditEmployeeViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.SaveSucceeded += ViewModel_SaveSucceeded;
        Loaded += EditEmployeeWindow_Loaded;
    }

    private async void EditEmployeeWindow_Loaded(
    object sender,
    RoutedEventArgs e)
    {
        await _viewModel.LoadOrganizationOptionsAsync();
    }

    public void LoadEmployee(Employee employee)
    {
        _viewModel.LoadEmployee(employee);
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
        Loaded -= EditEmployeeWindow_Loaded;

        base.OnClosed(e);
    }
}
