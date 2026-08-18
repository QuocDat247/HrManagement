using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Employees;
using HrManagement.Desktop.Services;
using HrManagement.Domain.Employees;
using HrManagement.Application.Employees.Profiles.Completion;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class EmployeesViewModel : ObservableObject
{
    private readonly IEmployeeProfileCompletionService _profileCompletionService;

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

    [ObservableProperty]
    private IReadOnlyList<EmployeeListItemViewModel>
        employeeItems =
            Array.Empty<EmployeeListItemViewModel>();

    [ObservableProperty]
    private EmployeeListItemViewModel?
        selectedEmployeeItem;

    private bool CanTransferEmployee()
    {
        return SelectedEmployee is
        {
            Status:
                EmployeeStatus.Active
                or EmployeeStatus.OnLeave
        };
    }

    private bool CanRehireEmployee()
    {
        return SelectedEmployee is
        {
            Status: EmployeeStatus.Inactive,
            TerminationDate: not null
        };
    }

    private bool CanViewEmploymentHistory()
    {
        return SelectedEmployee is not null;
    }

    private void ViewEmploymentHistory()
    {
        Employee? employee =
            SelectedEmployee;

        if (employee is null)
        {
            return;
        }

        _employeeDialogService
            .ShowEmploymentHistoryDialog(
                employee);
    }

    partial void OnSelectedEmployeeItemChanged(
    EmployeeListItemViewModel? value)
    {
        SelectedEmployee =
            value?.Employee;
    }

    public string Title => "Nhân viên";

    public IReadOnlyList<EmployeeStatusFilterOption> StatusOptions { get; } =
[
    new("Tất cả", null),
    new("Đang làm việc", EmployeeStatus.Active),
    new("Đang nghỉ", EmployeeStatus.OnLeave),
    new("Ngừng hoạt động", EmployeeStatus.Inactive)
];

    // Command
    public IAsyncRelayCommand SearchCommand { get; }
    public IAsyncRelayCommand ClearFiltersCommand { get; }
    public IAsyncRelayCommand AddEmployeeCommand { get; }
    public IAsyncRelayCommand EditEmployeeCommand { get; }
    public IAsyncRelayCommand DeactivateEmployeeCommand { get; }
    public IAsyncRelayCommand
    CancelDeactivationCommand
    {
        get;
    }
    public IAsyncRelayCommand RehireEmployeeCommand
    {
        get;
    }
    public IRelayCommand
    ViewEmploymentHistoryCommand
    {
        get;
    }

    public IAsyncRelayCommand
    TransferEmployeeCommand
    {
        get;
    }

    public IRelayCommand
    ViewOrganizationHistoryCommand
    {
        get;
    }

    public IAsyncRelayCommand
    ViewEmployeeProfileCommand
    {
        get;
    }

    // Constructor
    public EmployeesViewModel(
    IEmployeeService employeeService,
    IEmployeeDialogService employeeDialogService,
    IEmployeeProfileCompletionService profileCompletionService)
    {
        _employeeService =
            employeeService;

        _employeeDialogService =
            employeeDialogService;

        _profileCompletionService =
            profileCompletionService;

        ViewEmployeeProfileCommand =
            new AsyncRelayCommand(
                ViewEmployeeProfileAsync,
                CanViewEmployeeProfile);

        ViewOrganizationHistoryCommand =
            new RelayCommand(
                ViewOrganizationHistory,
                CanViewOrganizationHistory);

        TransferEmployeeCommand =
            new AsyncRelayCommand(
                TransferEmployeeAsync,
                CanTransferEmployee);

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

        CancelDeactivationCommand =
            new AsyncRelayCommand(
                CancelDeactivationAsync,
                CanCancelDeactivation);

        RehireEmployeeCommand =
            new AsyncRelayCommand(
                RehireEmployeeAsync,
                CanRehireEmployee);

        ViewEmploymentHistoryCommand =
            new RelayCommand(
                ViewEmploymentHistory,
                CanViewEmploymentHistory);

        SelectedStatusOption = StatusOptions[0];
    }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading =
                true;

            ErrorMessage =
                null;

            SelectedEmployeeItem =
                null;

            var filter =
                new EmployeeFilter(
                    SearchText,
                    SelectedStatusOption?.Status);

            IReadOnlyList<Employee> loadedEmployees =
                await _employeeService
                    .GetEmployeesAsync(
                        filter);

            EmployeeListItemViewModel[] items =
                await Task.WhenAll(
                    loadedEmployees.Select(
                        CreateEmployeeItemAsync));

            IEnumerable<EmployeeListItemViewModel>
                filteredItems =
                    items;

            if (RequiresProfileCompletionOnly)
            {
                filteredItems =
                    filteredItems.Where(
                        item =>
                            item.RequiresProfileCompletion);
            }

            EmployeeItems =
                filteredItems.ToArray();

            Employees =
                EmployeeItems
                    .Select(
                        item =>
                            item.Employee)
                    .ToArray();
        }
        catch (Exception)
        {
            EmployeeItems =
                Array.Empty<EmployeeListItemViewModel>();

            Employees =
                Array.Empty<Employee>();

            SelectedEmployeeItem =
                null;

            SelectedEmployee =
                null;

            ErrorMessage =
                "Không thể tải danh sách nhân viên.";
        }
        finally
        {
            IsLoading =
                false;
        }
    }

    private bool CanViewEmployeeProfile()
    {
        return SelectedEmployee is not null;
    }

    private async Task ViewEmployeeProfileAsync()
    {
        Employee? employee =
            SelectedEmployee;

        if (employee is null)
        {
            return;
        }

        _employeeDialogService
            .ShowEmployeeProfileDialog(
                employee);

        await LoadAsync();
    }

    private bool CanViewOrganizationHistory()
    {
        return SelectedEmployee is not null;
    }

    private void ViewOrganizationHistory()
    {
        Employee? employee =
            SelectedEmployee;

        if (employee is null)
        {
            return;
        }

        _employeeDialogService
            .ShowOrganizationHistoryDialog(
                employee);
    }

    private async Task TransferEmployeeAsync()
    {
        Employee? employee =
            SelectedEmployee;

        if (employee is null)
        {
            return;
        }

        bool saved =
            _employeeDialogService
                .ShowTransferEmployeeDialog(
                    employee);

        if (!saved)
        {
            return;
        }

        SelectedEmployee =
            null;

        await LoadAsync();
    }

    private async Task ClearFiltersAsync()
    {
        SearchText =
            null;

        SelectedStatusOption =
            StatusOptions[0];

        RequiresProfileCompletionOnly =
            false;

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

        CancelDeactivationCommand
        .NotifyCanExecuteChanged();

        RehireEmployeeCommand
        .NotifyCanExecuteChanged();

        ViewEmploymentHistoryCommand
        .NotifyCanExecuteChanged();

        TransferEmployeeCommand
        .NotifyCanExecuteChanged();

        ViewOrganizationHistoryCommand
        .NotifyCanExecuteChanged();

        ViewEmployeeProfileCommand
        .NotifyCanExecuteChanged();
    }

    private bool CanDeactivateEmployee()
    {
        return SelectedEmployee is not null
            && SelectedEmployee.Status != EmployeeStatus.Inactive;
    }

    private bool CanCancelDeactivation()
    {
        return SelectedEmployee is
        {
            Status: EmployeeStatus.Inactive,
            TerminationDate: not null
        };
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

    private async Task CancelDeactivationAsync()
    {
        Employee? employee =
            SelectedEmployee;

        if (employee is null
            || employee.Status
                != EmployeeStatus.Inactive
            || !employee.TerminationDate.HasValue)
        {
            return;
        }

        EmployeeStatus? restoredStatus =
            _employeeDialogService
                .ShowCancelEmployeeDeactivationDialog(
                    employee);

        if (!restoredStatus.HasValue)
        {
            return;
        }

        ErrorMessage = null;

        CancelEmployeeDeactivationResult result =
            await _employeeService
                .CancelDeactivationAsync(
                    employee.Id,
                    restoredStatus.Value);

        if (!result.IsSuccessful)
        {
            ErrorMessage =
                result.ErrorMessage;

            return;
        }

        SelectedEmployee = null;

        await LoadAsync();
    }

    private async Task RehireEmployeeAsync()
    {
        Employee? employee =
            SelectedEmployee;

        if (employee is null
            || employee.Status
                != EmployeeStatus.Inactive
            || !employee.TerminationDate.HasValue)
        {
            return;
        }

        RehireEmployeeDialogResult? dialogResult =
            _employeeDialogService
                .ShowRehireEmployeeDialog(
                    employee);

        if (dialogResult is null)
        {
            return;
        }

        ErrorMessage = null;

        RehireEmployeeResult result =
            await _employeeService
                .RehireEmployeeAsync(
                    employee.Id,
                    dialogResult.RehireDate,
                    dialogResult.RehireStatus);

        if (!result.IsSuccessful)
        {
            ErrorMessage =
                result.ErrorMessage;

            return;
        }

        SelectedEmployee = null;

        await LoadAsync();
    }

    private async Task<EmployeeListItemViewModel>
    CreateEmployeeItemAsync(
        Employee employee)
    {
        EmployeeProfileCompletionResult completion =
            await _profileCompletionService
                .EvaluateAsync(
                    employee);

        return new EmployeeListItemViewModel(
            employee,
            completion);
    }
}
