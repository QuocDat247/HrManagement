using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Employees;
using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class EmployeesViewModel : ObservableObject
{
    private readonly IEmployeeService _employeeService;

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

    public EmployeesViewModel(IEmployeeService employeeService)
    {
        _employeeService = employeeService;

        SearchCommand = new AsyncRelayCommand(LoadAsync);
        ClearFiltersCommand = new AsyncRelayCommand(ClearFiltersAsync);

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
                Status: SelectedStatusOption?.Status);

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
}
