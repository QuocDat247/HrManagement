using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Domain.Employees;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class CancelEmployeeDeactivationViewModel
    : ObservableObject
{
    [ObservableProperty]
    private string employeeCode = string.Empty;

    [ObservableProperty]
    private string fullName = string.Empty;

    [ObservableProperty]
    private DateTime terminationDate;

    [ObservableProperty]
    private EmployeeRestoreStatusOption
        selectedStatusOption = default!;

    [ObservableProperty]
    private string? errorMessage;

    public IReadOnlyList<EmployeeRestoreStatusOption>
        StatusOptions
    { get; } =
    [
        new(
            "Đang làm việc",
            EmployeeStatus.Active),

        new(
            "Nghỉ phép",
            EmployeeStatus.OnLeave)
    ];

    public event EventHandler? ConfirmSucceeded;

    public IRelayCommand ConfirmCommand { get; }

    public CancelEmployeeDeactivationViewModel()
    {
        SelectedStatusOption =
            StatusOptions[0];

        ConfirmCommand =
            new RelayCommand(Confirm);
    }

    public void LoadEmployee(Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        EmployeeCode =
            employee.EmployeeCode;

        FullName =
            employee.FullName;

        TerminationDate =
            employee.TerminationDate.HasValue
                ? employee.TerminationDate.Value.ToDateTime(
                    TimeOnly.MinValue)
                : default;

        SelectedStatusOption =
            StatusOptions[0];

        ErrorMessage = null;
    }

    private void Confirm()
    {
        if (TerminationDate == default)
        {
            ErrorMessage =
                "Nhân viên chưa có ngày nghỉ việc để hủy.";

            return;
        }

        ErrorMessage = null;

        ConfirmSucceeded?.Invoke(
            this,
            EventArgs.Empty);
    }
}
