using CommunityToolkit.Mvvm.ComponentModel;
using HrManagement.Application.Employees.Profiles;
using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class EmployeeAddressSectionViewModel
    : ObservableObject
{
    private readonly IEmployeeAddressService
        _addressService;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    public EmployeeAddressSlotViewModel
        PermanentAddress
    {
        get;
    }

    public EmployeeAddressSlotViewModel
        CurrentAddress
    {
        get;
    }

    public EmployeeAddressSectionViewModel(
        IEmployeeAddressService addressService)
    {
        _addressService =
            addressService;

        PermanentAddress =
            new EmployeeAddressSlotViewModel(
                addressService,
                EmployeeAddressType.Permanent,
                "Địa chỉ thường trú");

        CurrentAddress =
            new EmployeeAddressSlotViewModel(
                addressService,
                EmployeeAddressType.Current,
                "Địa chỉ hiện tại");
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

        IsLoading =
            true;

        ErrorMessage =
            null;

        try
        {
            EmployeeAddressBookDetails details =
                await _addressService
                    .GetAddressesAsync(
                        employeeId,
                        cancellationToken);

            PermanentAddress.Load(
                employeeId,
                details.PermanentAddress);

            CurrentAddress.Load(
                employeeId,
                details.CurrentAddress);
        }
        catch (Exception)
        {
            PermanentAddress.Load(
                employeeId,
                null);

            CurrentAddress.Load(
                employeeId,
                null);

            ErrorMessage =
                "Không thể tải thông tin địa chỉ.";
        }
        finally
        {
            IsLoading =
                false;
        }
    }
}
