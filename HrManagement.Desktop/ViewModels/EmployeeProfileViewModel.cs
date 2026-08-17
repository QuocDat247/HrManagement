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

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isLoaded;

    [ObservableProperty]
    private string? errorMessage;

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

    public EmployeeIdentificationRecordSectionViewModel
    IdentificationRecords
    {
        get;
    }
    public EmployeeProfileViewModel(
    EmployeePersonalProfileSectionViewModel
        personalInformation,
    EmployeeAddressSectionViewModel
        addresses,
    EmployeeEmergencyContactSectionViewModel
        emergencyContacts,
    EmployeeIdentificationRecordSectionViewModel
        identificationRecords)
    {
        PersonalInformation =
            personalInformation;

        Addresses =
            addresses;

        EmergencyContacts =
            emergencyContacts;

        IdentificationRecords =
            identificationRecords;
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

        IsLoading =
            true;

        IsLoaded =
            false;

        ErrorMessage =
            null;

        try
        {
            Task personalTask =
                PersonalInformation
                    .LoadAsync(
                        employee.Id,
                        cancellationToken);

            Task addressTask =
                Addresses
                    .LoadAsync(
                        employee.Id,
                        cancellationToken);

            Task emergencyContactTask =
                EmergencyContacts
                    .LoadAsync(
                        employee.Id,
                        cancellationToken);

            Task identificationTask =
                IdentificationRecords
                    .LoadAsync(
                        employee.Id,
                        cancellationToken);

            await Task.WhenAll(
                personalTask,
                addressTask,
                emergencyContactTask,
                identificationTask);

            IsLoaded =
                true;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể tải đầy đủ hồ sơ nhân viên.";
        }
        finally
        {
            IsLoading =
                false;
        }
    }
}
