using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Employees;
using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class EditEmployeeViewModel : ObservableObject
{
    private readonly IEmployeeService _employeeService;

    private Guid _employeeId;

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

    [ObservableProperty]
    private IReadOnlyList<EmployeeStatus> statusOptions =
    [
        EmployeeStatus.Active,
        EmployeeStatus.OnLeave
    ];

    public IAsyncRelayCommand SaveCommand { get; }

    public event EventHandler? SaveSucceeded;

    public EditEmployeeViewModel(IEmployeeService employeeService)
    {
        _employeeService = employeeService;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    public void LoadEmployee(Employee employee)
    {
        _employeeId = employee.Id;

        EmployeeCode = employee.EmployeeCode;
        FullName = employee.FullName;
        Email = employee.Email;
        PhoneNumber = employee.PhoneNumber;

        DateOfBirth = employee.DateOfBirth.HasValue
            ? employee.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue)
            : null;

        HireDate =
            employee.HireDate.ToDateTime(TimeOnly.MinValue);

        Department = employee.Department;
        Position = employee.Position;
        StatusOptions =
        employee.Status == EmployeeStatus.Inactive
        ? [EmployeeStatus.Inactive]
        :
        [
            EmployeeStatus.Active,
            EmployeeStatus.OnLeave
        ];

        SelectedStatus = employee.Status;
        SelectedStatus = employee.Status;

        ErrorMessage = null;
    }

    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var request = new UpdateEmployeeRequest(
                _employeeId,
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

            UpdateEmployeeResult result =
                await _employeeService.UpdateEmployeeAsync(request);

            if (!result.IsSuccessful)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }

            SaveSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            ErrorMessage = "Không thể cập nhật nhân viên.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
