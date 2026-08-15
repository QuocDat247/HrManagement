using HrManagement.Desktop.ViewModels;
using System.Windows;
using DepartmentEntity =
    HrManagement.Domain.Organization.Departments.Department;
using PositionEntity =
    HrManagement.Domain.Organization.Positions.Position;

namespace HrManagement.Desktop.Views;

public partial class OrganizationEmployeesWindow
    : Window
{
    private readonly OrganizationEmployeesViewModel
        _viewModel;

    private bool _loaded;

    public OrganizationEmployeesWindow(
        OrganizationEmployeesViewModel viewModel)
    {
        InitializeComponent();

        _viewModel =
            viewModel;

        DataContext =
            viewModel;
    }

    public void LoadDepartment(
        DepartmentEntity department)
    {
        _viewModel.ConfigureForDepartment(
            department);

        _loaded = false;
    }

    public void LoadPosition(
        PositionEntity position)
    {
        _viewModel.ConfigureForPosition(
            position);

        _loaded = false;
    }

    private async void OrganizationEmployeesWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;

        await _viewModel.LoadAsync();
    }
}
