using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Employees.Profiles;
using HrManagement.Domain.Employees.Profiles;
using System.Collections.ObjectModel;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class EmployeePersonalProfileSectionViewModel
    : ObservableObject
{
    private readonly IEmployeePersonalProfileService
        _profileService;

    private Guid _employeeId;

    [ObservableProperty]
    private string? preferredName;

    [ObservableProperty]
    private EmployeeGenderOption? selectedGender;

    [ObservableProperty]
    private string? nationality;

    [ObservableProperty]
    private string? placeOfBirth;

    [ObservableProperty]
    private bool hasProfile;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string? successMessage;

    public ObservableCollection<EmployeeGenderOption>
        GenderOptions
    {
        get;
    } =
    [
        new(
            EmployeeGender.Male,
            "Nam"),

        new(
            EmployeeGender.Female,
            "Nữ"),

        new(
            EmployeeGender.Other,
            "Khác")
    ];

    public IAsyncRelayCommand SaveCommand
    {
        get;
    }

    public EmployeePersonalProfileSectionViewModel(
        IEmployeePersonalProfileService profileService)
    {
        _profileService =
            profileService;

        SaveCommand =
            new AsyncRelayCommand(
                SaveAsync,
                CanSave);
    }

    public async Task LoadAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        _employeeId =
            employeeId;

        IsLoading =
            true;

        ErrorMessage =
            null;

        SuccessMessage =
            null;

        try
        {
            EmployeePersonalProfileDetails details =
                await _profileService
                    .GetProfileAsync(
                        employeeId,
                        cancellationToken);

            HasProfile =
                details.HasProfile;

            PreferredName =
                details.PreferredName;

            SelectedGender =
                details.Gender.HasValue
                    ? GenderOptions
                        .Single(
                            option =>
                                option.Value ==
                                details.Gender.Value)
                    : null;

            Nationality =
                details.Nationality;

            PlaceOfBirth =
                details.PlaceOfBirth;
        }
        catch (Exception)
        {
            HasProfile =
                false;

            PreferredName =
                null;

            SelectedGender =
                null;

            Nationality =
                null;

            PlaceOfBirth =
                null;

            ErrorMessage =
                "Không thể tải thông tin cá nhân mở rộng.";
        }
        finally
        {
            IsLoading =
                false;

            SaveCommand
                .NotifyCanExecuteChanged();
        }
    }

    private bool CanSave()
    {
        return _employeeId != Guid.Empty
            && !IsLoading
            && !IsSaving;
    }

    private async Task SaveAsync()
    {
        if (!CanSave())
        {
            return;
        }

        IsSaving =
            true;

        ErrorMessage =
            null;

        SuccessMessage =
            null;

        SaveCommand
            .NotifyCanExecuteChanged();

        try
        {
            SaveEmployeePersonalProfileResult result =
                await _profileService
                    .SaveProfileAsync(
                        new SaveEmployeePersonalProfileRequest(
                            _employeeId,
                            PreferredName,
                            SelectedGender?.Value,
                            Nationality,
                            PlaceOfBirth));

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể lưu thông tin cá nhân.";

                return;
            }

            HasProfile =
                true;

            SuccessMessage =
                "Đã lưu thông tin cá nhân.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể lưu thông tin cá nhân.";
        }
        finally
        {
            IsSaving =
                false;

            SaveCommand
                .NotifyCanExecuteChanged();
        }
    }
}
