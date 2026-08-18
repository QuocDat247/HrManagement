using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Employees.Profiles;
using HrManagement.Desktop.Services;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class EmployeeEmergencyContactSectionViewModel
    : ObservableObject
{
    private readonly IConfirmationDialogService
        _confirmationDialogService;

    private readonly IEmployeeEmergencyContactService
        _contactService;

    private Guid _employeeId;

    public ObservableCollection<EmployeeEmergencyContactDetails>
        Contacts
    {
        get;
    } = [];

    [ObservableProperty]
    private EmployeeEmergencyContactDetails?
        selectedContact;

    [ObservableProperty]
    private Guid? editingContactId;

    [ObservableProperty]
    private string? fullName;

    [ObservableProperty]
    private string? relationship;

    [ObservableProperty]
    private string? phoneNumber;

    [ObservableProperty]
    private string? email;

    [ObservableProperty]
    private bool isPrimary;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string? successMessage;

    public event EventHandler?
        ProfileDataChanged;

    public IRelayCommand NewCommand
    {
        get;
    }

    public IAsyncRelayCommand SaveCommand
    {
        get;
    }

    public IAsyncRelayCommand DeleteCommand
    {
        get;
    }

    public EmployeeEmergencyContactSectionViewModel(
        IEmployeeEmergencyContactService contactService, IConfirmationDialogService confirmationDialogService)
    {
        _contactService =
            contactService;

        _confirmationDialogService =
            confirmationDialogService;

        NewCommand =
            new RelayCommand(
                StartNew,
                CanStartNew);

        SaveCommand =
            new AsyncRelayCommand(
                SaveAsync,
                CanSave);

        DeleteCommand =
            new AsyncRelayCommand(
                DeleteAsync,
                CanDelete);
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

        IsBusy =
            true;

        ErrorMessage =
            null;

        SuccessMessage =
            null;

        NotifyCommands();

        try
        {
            await RefreshContactsAsync(
                cancellationToken);

            if (Contacts.Count > 0)
            {
                SelectedContact =
                    Contacts[0];
            }
            else
            {
                ResetEditor();
            }
        }
        catch (Exception)
        {
            Contacts.Clear();

            ResetEditor();

            ErrorMessage =
                "Không thể tải thông tin liên hệ khẩn cấp.";
        }
        finally
        {
            IsBusy =
                false;

            NotifyCommands();
        }
    }

    private void StartNew()
    {
        SelectedContact =
            null;

        ResetEditor();

        ErrorMessage =
            null;

        SuccessMessage =
            null;
    }

    private bool CanStartNew()
    {
        return _employeeId != Guid.Empty
            && !IsBusy;
    }

    private bool CanSave()
    {
        return _employeeId != Guid.Empty
            && !IsBusy;
    }

    private bool CanDelete()
    {
        return _employeeId != Guid.Empty
            && EditingContactId.HasValue
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
            EmployeeEmergencyContactOperationResult result =
                await _contactService
                    .SaveContactAsync(
                        new SaveEmployeeEmergencyContactRequest(
                            _employeeId,
                            EditingContactId,
                            FullName ?? string.Empty,
                            Relationship ?? string.Empty,
                            PhoneNumber ?? string.Empty,
                            Email,
                            IsPrimary));

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể lưu liên hệ khẩn cấp.";

                return;
            }

            await RefreshContactsAsync();

            SelectedContact =
                result.ContactId.HasValue
                    ? Contacts.FirstOrDefault(
                        contact =>
                            contact.Id ==
                            result.ContactId.Value)
                    : null;

            SuccessMessage =
                "Đã lưu liên hệ khẩn cấp.";

            ProfileDataChanged?.Invoke(
                this,
                EventArgs.Empty);
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể lưu liên hệ khẩn cấp.";
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
        if (!CanDelete()
            || !EditingContactId.HasValue)
        {
            return;
        }

        string contactName =
            string.IsNullOrWhiteSpace(
                FullName)
                ? "liên hệ này"
                : FullName.Trim();

        bool confirmed =
            _confirmationDialogService.Confirm(
                "Xác nhận xóa liên hệ",
                $"Bạn có muốn xóa liên hệ khẩn cấp “{contactName}” không?");

        if (!confirmed)
        {
            return;
        }

        Guid contactId =
            EditingContactId.Value;

        IsBusy =
            true;

        ErrorMessage =
            null;

        SuccessMessage =
            null;

        NotifyCommands();

        try
        {
            EmployeeEmergencyContactOperationResult result =
                await _contactService
                    .DeleteContactAsync(
                        _employeeId,
                        contactId);

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể xóa liên hệ khẩn cấp.";

                return;
            }

            await RefreshContactsAsync();

            SelectedContact =
                null;

            ResetEditor();

            SuccessMessage =
                "Đã xóa liên hệ khẩn cấp.";

            ProfileDataChanged?.Invoke(
                this,
                EventArgs.Empty);
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể xóa liên hệ khẩn cấp.";
        }
        finally
        {
            IsBusy =
                false;

            NotifyCommands();
        }
    }

    private async Task RefreshContactsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EmployeeEmergencyContactDetails> contacts =
            await _contactService
                .GetContactsAsync(
                    _employeeId,
                    cancellationToken);

        Contacts.Clear();

        foreach (EmployeeEmergencyContactDetails contact
                 in contacts)
        {
            Contacts.Add(
                contact);
        }
    }

    private void ResetEditor()
    {
        EditingContactId =
            null;

        FullName =
            null;

        Relationship =
            null;

        PhoneNumber =
            null;

        Email =
            null;

        IsPrimary =
            false;

        NotifyCommands();
    }

    partial void OnSelectedContactChanged(
        EmployeeEmergencyContactDetails? value)
    {
        ErrorMessage =
            null;

        SuccessMessage =
            null;

        if (value is null)
        {
            return;
        }

        EditingContactId =
            value.Id;

        FullName =
            value.FullName;

        Relationship =
            value.Relationship;

        PhoneNumber =
            value.PhoneNumber;

        Email =
            value.Email;

        IsPrimary =
            value.IsPrimary;

        NotifyCommands();
    }

    partial void OnEditingContactIdChanged(
        Guid? value)
    {
        DeleteCommand
            .NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(
        bool value)
    {
        NotifyCommands();
    }

    private void NotifyCommands()
    {
        NewCommand
            .NotifyCanExecuteChanged();

        SaveCommand
            .NotifyCanExecuteChanged();

        DeleteCommand
            .NotifyCanExecuteChanged();
    }
}
