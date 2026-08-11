using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Employees;
using HrManagement.Desktop.Services;
using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class EmployeesViewModel : ObservableObject
{
    private readonly IEmployeeService _employeeService;
    private readonly IEmployeeDialogService _employeeDialogService;

    [ObservableProperty]
    private IReadOnlyList<Employee> employees =
        Array.Empty<Employee>();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string? searchText;

    [ObservableProperty]
    private EmployeeStatusFilterOption? selectedStatusOption;

    [ObservableProperty]
    private Employee? selectedEmployee;

    [ObservableProperty]
    private bool requiresProfileCompletionOnly;

    public string Title => "Nhân viên";

    public IReadOnlyList<EmployeeStatusFilterOption> StatusOptions { get; } =
[
    new("Tất cả", null),
    new("Đang làm việc", EmployeeStatus.Active),
    new("Đang nghỉ", EmployeeStatus.OnLeave),
    new("Ngừng hoạt động", EmployeeStatus.Inactive)
];

    public IAsyncRelayCommand SearchCommand { get; }
    public IAsyncRelayCommand ClearFiltersCommand { get; }
    public IAsyncRelayCommand AddEmployeeCommand { get; }
    public IAsyncRelayCommand EditEmployeeCommand { get; }
    public IAsyncRelayCommand DeactivateEmployeeCommand { get; }

    public EmployeesViewModel(
    IEmployeeService employeeService,
    IEmployeeDialogService employeeDialogService)
    {
        _employeeService = employeeService;
        _employeeDialogService = employeeDialogService;

        SearchCommand = new AsyncRelayCommand(LoadAsync);
        ClearFiltersCommand =
            new AsyncRelayCommand(ClearFiltersAsync);

        AddEmployeeCommand =
            new AsyncRelayCommand(AddEmployeeAsync);

        EditEmployeeCommand =
            new AsyncRelayCommand(
                EditEmployeeAsync,
                CanEditEmployee);

        DeactivateEmployeeCommand =
            new AsyncRelayCommand(
                DeactivateEmployeeAsync,
                CanDeactivateEmployee);

        SelectedStatusOption = StatusOptions[0];
    }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var filter = new EmployeeFilter(
                SearchText: SearchText,
                Status: SelectedStatusOption?.Status,
                RequiresProfileCompletionOnly: RequiresProfileCompletionOnly);

            Employees =
                await _employeeService.GetEmployeesAsync(filter);
        }
        catch (Exception)
        {
            Employees = Array.Empty<Employee>();
            ErrorMessage = "Không thể tải danh sách nhân viên.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ClearFiltersAsync()
    {
        SearchText = null;
        SelectedStatusOption = StatusOptions[0];

        await LoadAsync();
    }

    private async Task AddEmployeeAsync()
    {
        bool saved =
            _employeeDialogService.ShowAddEmployeeDialog();

        if (!saved)
        {
            return;
        }

        await LoadAsync();
    }

    private bool CanEditEmployee()
    {
        return SelectedEmployee is not null;
    }

    private async Task EditEmployeeAsync()
    {
        if (SelectedEmployee is null)
        {
            return;
        }

        bool saved =
            _employeeDialogService.ShowEditEmployeeDialog(
                SelectedEmployee);

        if (!saved)
        {
            return;
        }

        await LoadAsync();

        SelectedEmployee = null;
    }

    partial void OnSelectedEmployeeChanged(Employee? value)
    {
        EditEmployeeCommand.NotifyCanExecuteChanged();
        DeactivateEmployeeCommand.NotifyCanExecuteChanged();
    }

    private bool CanDeactivateEmployee()
    {
        return SelectedEmployee is not null
            && SelectedEmployee.Status != EmployeeStatus.Inactive;
    }

    private async Task DeactivateEmployeeAsync()
    {
        if (SelectedEmployee is null)
        {
            return;
        }

        Employee employee = SelectedEmployee;

        DateOnly? terminationDate =
        _employeeDialogService
        .ShowDeactivateEmployeeDialog(employee);

        if (!terminationDate.HasValue)
        {
            return;
        }

        try
        {
            ErrorMessage = null;

            DeactivateEmployeeResult result =
                await _employeeService
                    .DeactivateEmployeeAsync(
                        employee.Id,
                        terminationDate.Value);

            if (!result.IsSuccessful)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }

            await LoadAsync();

            SelectedEmployee = null;
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể ngừng hoạt động nhân viên.";
        }
    }

    public async Task ShowProfileCompletionRequiredAsync()
    {
        SearchText = null;
        SelectedStatusOption = StatusOptions[0];
        RequiresProfileCompletionOnly = true;

        await LoadAsync();
    }
}
