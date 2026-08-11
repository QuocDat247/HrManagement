using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class DeactivateEmployeeViewModel
    : ObservableObject
{
    [ObservableProperty]
    private string employeeCode = string.Empty;

    [ObservableProperty]
    private string fullName = string.Empty;

    [ObservableProperty]
    private DateTime hireDate;

    [ObservableProperty]
    private DateTime terminationDate = DateTime.Today;

    [ObservableProperty]
    private string? errorMessage;

    public DateTime Today { get; } =
        DateTime.Today;

    public IRelayCommand ConfirmCommand { get; }

    public event EventHandler? ConfirmSucceeded;

    public DeactivateEmployeeViewModel()
    {
        ConfirmCommand =
            new RelayCommand(Confirm);
    }

    public void LoadEmployee(Employee employee)
    {
        EmployeeCode = employee.EmployeeCode;
        FullName = employee.FullName;

        HireDate =
            employee.HireDate.ToDateTime(
                TimeOnly.MinValue);

        TerminationDate =
            DateTime.Today;

        ErrorMessage = null;
    }

    private void Confirm()
    {
        ErrorMessage = null;

        if (TerminationDate.Date < HireDate.Date)
        {
            ErrorMessage =
                "Ngày nghỉ việc không thể trước ngày vào làm.";

            return;
        }

        if (TerminationDate.Date > Today.Date)
        {
            ErrorMessage =
                "Ngày nghỉ việc không thể ở tương lai.";

            return;
        }

        ConfirmSucceeded?.Invoke(
            this,
            EventArgs.Empty);
    }
}
