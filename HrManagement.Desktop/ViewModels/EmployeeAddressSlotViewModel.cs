using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Employees.Profiles;
using HrManagement.Desktop.Services;
using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class EmployeeAddressSlotViewModel
    : ObservableObject
{
    private readonly IConfirmationDialogService
        _confirmationDialogService;

    private readonly IEmployeeAddressService
        _addressService;

    private Guid _employeeId;

    public EmployeeAddressType Type
    {
        get;
    }

    public string Title
    {
        get;
    }

    [ObservableProperty]
    private string? addressLine;

    [ObservableProperty]
    private string? ward;

    [ObservableProperty]
    private string? district;

    [ObservableProperty]
    private string? province;

    [ObservableProperty]
    private string country =
        "Việt Nam";

    [ObservableProperty]
    private string? postalCode;

    [ObservableProperty]
    private bool hasAddress;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string? successMessage;

    public IAsyncRelayCommand SaveCommand
    {
        get;
    }

    public IAsyncRelayCommand DeleteCommand
    {
        get;
    }

    public EmployeeAddressSlotViewModel(
    IEmployeeAddressService addressService,
    EmployeeAddressType type,
    string title,
    IConfirmationDialogService confirmationDialogService)
    {
        _addressService =
            addressService;

        _confirmationDialogService =
            confirmationDialogService;

        Type =
            type;

        Title =
            title;

        SaveCommand =
            new AsyncRelayCommand(
                SaveAsync,
                CanSave);

        DeleteCommand =
            new AsyncRelayCommand(
                DeleteAsync,
                CanDelete);
    }

    public void Load(
        Guid employeeId,
        EmployeeAddressDetails? address)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        _employeeId =
            employeeId;

        ErrorMessage =
            null;

        SuccessMessage =
            null;

        if (address is null)
        {
            ClearFields();

            HasAddress =
                false;

            NotifyCommands();

            return;
        }

        AddressLine =
            address.AddressLine;

        Ward =
            address.Ward;

        District =
            address.District;

        Province =
            address.Province;

        Country =
            address.Country;

        PostalCode =
            address.PostalCode;

        HasAddress =
            true;

        NotifyCommands();
    }

    private bool CanSave()
    {
        return _employeeId != Guid.Empty
            && !IsBusy;
    }

    private bool CanDelete()
    {
        return _employeeId != Guid.Empty
            && HasAddress
            && !IsBusy;
    }

    private async Task SaveAsync()
    {
        if (!CanSave())
        {
            return;
        }

        IsBusy =
            true;

        ErrorMessage =
            null;

        SuccessMessage =
            null;

        NotifyCommands();

        try
        {
            EmployeeAddressOperationResult result =
                await _addressService
                    .SaveAddressAsync(
                        new SaveEmployeeAddressRequest(
                            _employeeId,
                            Type,
                            AddressLine ?? string.Empty,
                            Ward,
                            District,
                            Province,
                            Country,
                            PostalCode));

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể lưu địa chỉ.";

                return;
            }

            HasAddress =
                true;

            SuccessMessage =
                "Đã lưu địa chỉ.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể lưu địa chỉ.";
        }
        finally
        {
            IsBusy =
                false;

            NotifyCommands();
        }
    }

    private async Task DeleteAsync()
    {
        if (!CanDelete())
        {
            return;
        }

        string confirmationMessage =
        Type switch
        {
            EmployeeAddressType.Permanent =>
                "Bạn có muốn xóa thông tin địa chỉ thường trú hiện tại không?",

            EmployeeAddressType.Current =>
                "Bạn có muốn xóa thông tin địa chỉ hiện tại không?",

            _ =>
                "Bạn có muốn xóa thông tin địa chỉ này không?"
        };

        bool confirmed =
            _confirmationDialogService.Confirm(
                "Xác nhận xóa địa chỉ",
                confirmationMessage);

        if (!confirmed)
        {
            return;
        }

        IsBusy =
            true;

        ErrorMessage =
            null;

        SuccessMessage =
            null;

        NotifyCommands();

        try
        {
            EmployeeAddressOperationResult result =
                await _addressService
                    .DeleteAddressAsync(
                        _employeeId,
                        Type);

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể xóa địa chỉ.";

                return;
            }

            ClearFields();

            HasAddress =
                false;

            SuccessMessage =
                "Đã xóa địa chỉ.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể xóa địa chỉ.";
        }
        finally
        {
            IsBusy =
                false;

            NotifyCommands();
        }
    }

    private void ClearFields()
    {
        AddressLine =
            null;

        Ward =
            null;

        District =
            null;

        Province =
            null;

        Country =
            "Việt Nam";

        PostalCode =
            null;
    }

    private void NotifyCommands()
    {
        SaveCommand
            .NotifyCanExecuteChanged();

        DeleteCommand
            .NotifyCanExecuteChanged();
    }
}
