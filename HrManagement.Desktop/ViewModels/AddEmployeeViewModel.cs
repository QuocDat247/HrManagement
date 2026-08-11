using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Employees;
using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class AddEmployeeViewModel : ObservableObject
{
    private readonly IEmployeeService _employeeService;

    [ObservableProperty]
    private string employeeCode = string.Empty;

    [ObservableProperty]
    private string fullName = string.Empty;

    [ObservableProperty]
    private string? email;

    [ObservableProperty]
    private string? phoneNumber;

    [ObservableProperty]
    private DateTime? dateOfBirth;

    [ObservableProperty]
    private DateTime hireDate = DateTime.Today;

    [ObservableProperty]
    private string department = string.Empty;

    [ObservableProperty]
    private string position = string.Empty;

    [ObservableProperty]
    private EmployeeStatus selectedStatus = EmployeeStatus.Active;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isBusy;

    public IReadOnlyList<EmployeeStatus> StatusOptions { get; } =
    [
        EmployeeStatus.Active,
        EmployeeStatus.OnLeave
    ];

    public IAsyncRelayCommand SaveCommand { get; }

    public event EventHandler? SaveSucceeded;

    public AddEmployeeViewModel(IEmployeeService employeeService)
    {
        _employeeService = employeeService;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var request = new CreateEmployeeRequest(
                EmployeeCode,
                FullName,
                Email,
                PhoneNumber,
                DateOfBirth.HasValue
                    ? DateOnly.FromDateTime(DateOfBirth.Value)
                    : null,
                DateOnly.FromDateTime(HireDate),
                Department,
                Position,
                SelectedStatus);

            CreateEmployeeResult result =
                await _employeeService.CreateEmployeeAsync(request);

            if (!result.IsSuccessful)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }

            SaveSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            ErrorMessage = "Không thể thêm nhân viên.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
