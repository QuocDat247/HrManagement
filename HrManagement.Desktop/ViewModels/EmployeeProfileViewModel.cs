using CommunityToolkit.Mvvm.ComponentModel;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class EmployeeProfileViewModel
    : ObservableObject
{
    [ObservableProperty]
    private string employeeCode =
        string.Empty;

    [ObservableProperty]
    private string fullName =
        string.Empty;

    [ObservableProperty]
    private string department =
        string.Empty;

    [ObservableProperty]
    private string position =
        string.Empty;

    [ObservableProperty]
    private string statusText =
        string.Empty;

    public EmployeeAddressSectionViewModel
    Addresses
    {
        get;
    }

    public EmployeePersonalProfileSectionViewModel
        PersonalInformation
    {
        get;
    }

    public EmployeeEmergencyContactSectionViewModel
    EmergencyContacts
    {
        get;
    }
    public EmployeeProfileViewModel(
        EmployeePersonalProfileSectionViewModel
            personalInformation,
        EmployeeAddressSectionViewModel
            addresses,
        EmployeeEmergencyContactSectionViewModel
        emergencyContacts)
    {
        PersonalInformation =
            personalInformation;

        Addresses =
            addresses;

        EmergencyContacts =
            emergencyContacts;
    }

    public async Task LoadEmployeeAsync(
    Employee employee,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            employee);

        EmployeeCode =
            employee.EmployeeCode;

        FullName =
            employee.FullName;

        Department =
            employee.Department;

        Position =
            employee.Position;

        StatusText =
    employee.Status switch
    {
        EmployeeStatus.Active =>
            "Đang làm việc",

        EmployeeStatus.OnLeave =>
            "Tạm nghỉ",

        EmployeeStatus.Inactive =>
            "Đã nghỉ việc",

        _ =>
            employee.Status.ToString()
    };


        await PersonalInformation
            .LoadAsync(
                employee.Id,
                cancellationToken);

        await Addresses
            .LoadAsync(
                employee.Id,
                cancellationToken);

        await EmergencyContacts
            .LoadAsync(
                employee.Id,
                cancellationToken);
    }
}
