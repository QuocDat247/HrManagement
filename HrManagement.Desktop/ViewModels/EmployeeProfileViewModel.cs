using CommunityToolkit.Mvvm.ComponentModel;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Organization.Departments;
using HrManagement.Domain.Organization.Positions;
using HrManagement.Application.Employees.Profiles.Completion;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class EmployeeProfileViewModel
    : ObservableObject
{
    private readonly IEmployeeProfileCompletionService
        _profileCompletionService;

    private Employee?
        _employee;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsProfileComplete))]
    [NotifyPropertyChangedFor(nameof(RequiresProfileCompletion))]
    [NotifyPropertyChangedFor(nameof(HasMissingProfileInformation))]
    [NotifyPropertyChangedFor(nameof(CompletionStatusText))]
    [NotifyPropertyChangedFor(nameof(CompletionDetailsText))]
    private EmployeeProfileCompletionResult?
        completion;

    [ObservableProperty]
    private string?
        completionErrorMessage;

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

    public bool IsProfileComplete =>
        Completion?.IsComplete == true;

    public bool RequiresProfileCompletion =>
        Completion?.RequiresCompletion == true;

    public bool HasMissingProfileInformation =>
        Completion is not null
        && Completion.MissingRequirements.Count > 0;

    public string CompletionStatusText =>
        Completion is null
            ? string.Empty
            : EmployeeProfileCompletionPresentation
                .BuildStatusText(
                    Completion);

    public string CompletionDetailsText =>
        Completion is null
            ? string.Empty
            : EmployeeProfileCompletionPresentation
                .BuildMissingText(
                    Completion);

    public EmployeeProfileViewModel(
        EmployeePersonalProfileSectionViewModel
            personalInformation,
        EmployeeAddressSectionViewModel
            addresses,
        EmployeeEmergencyContactSectionViewModel
            emergencyContacts,
        EmployeeIdentificationRecordSectionViewModel
            identificationRecords,
        IEmployeeProfileCompletionService
            profileCompletionService)
    {
        PersonalInformation =
            personalInformation;

        Addresses =
            addresses;

        EmergencyContacts =
            emergencyContacts;

        IdentificationRecords =
            identificationRecords;

        _profileCompletionService =
            profileCompletionService;

        PersonalInformation.ProfileDataChanged +=
            OnProfileDataChanged;

        Addresses.ProfileDataChanged +=
            OnProfileDataChanged;

        EmergencyContacts.ProfileDataChanged +=
            OnProfileDataChanged;

        IdentificationRecords.ProfileDataChanged +=
            OnProfileDataChanged;
    }

    public async Task LoadEmployeeAsync(
        Employee employee,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            employee);

        _employee =
            employee;

        Completion =
            null;
        CompletionErrorMessage =
            null;

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

            await RefreshCompletionAsync(
                cancellationToken);

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

    public async Task RefreshCompletionAsync(
        CancellationToken cancellationToken = default)
    {
        Employee? employee =
            _employee;

        if (employee is null)
        {
            Completion =
                null;

            return;
        }

        CompletionErrorMessage =
            null;

        try
        {
            Completion =
                await _profileCompletionService
                    .EvaluateAsync(
                        employee,
                        cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            Completion =
                null;

            CompletionErrorMessage =
                "Không thể đánh giá độ đầy đủ của hồ sơ.";
        }
    }

    private async void OnProfileDataChanged(
        object? sender,
        EventArgs e)
    {
        await RefreshCompletionAsync();
    }
}
