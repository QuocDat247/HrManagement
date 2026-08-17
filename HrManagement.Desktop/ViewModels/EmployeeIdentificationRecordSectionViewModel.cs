using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Employees.Profiles;
using HrManagement.Desktop.Services;
using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class EmployeeIdentificationRecordSectionViewModel
    : ObservableObject
{
    private readonly IConfirmationDialogService
        _confirmationDialogService;

    private readonly IEmployeeIdentificationRecordService
        _recordService;

    private Guid _employeeId;

    public ObservableCollection<EmployeeIdentificationRecordDetails>
        Records
    {
        get;
    } = [];

    public IReadOnlyList<EmployeeIdentificationTypeOption>
        TypeOptions
    {
        get;
    }

    [ObservableProperty]
    private EmployeeIdentificationRecordDetails?
        selectedRecord;

    [ObservableProperty]
    private Guid? editingRecordId;

    [ObservableProperty]
    private EmployeeIdentificationTypeOption?
        selectedType;

    [ObservableProperty]
    private string? documentNumber;

    [ObservableProperty]
    private DateTime? issueDate;

    [ObservableProperty]
    private DateTime? expiryDate;

    [ObservableProperty]
    private string? issuingAuthority;

    [ObservableProperty]
    private string? placeOfIssue;

    [ObservableProperty]
    private string? issuingCountry;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string? successMessage;

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

    public EmployeeIdentificationRecordSectionViewModel(
        IEmployeeIdentificationRecordService recordService, IConfirmationDialogService confirmationDialogService)
    {
        _recordService =
            recordService;

        _confirmationDialogService =
            confirmationDialogService;

        TypeOptions =
        [
            new(
                EmployeeIdentificationType.NationalId,
                "CCCD/CMND"),

            new(
                EmployeeIdentificationType.Passport,
                "Hộ chiếu"),

            new(
                EmployeeIdentificationType.Other,
                "Khác")
        ];

        SelectedType =
            TypeOptions[0];

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
            await RefreshRecordsAsync(
                cancellationToken);

            if (Records.Count > 0)
            {
                SelectedRecord =
                    Records[0];
            }
            else
            {
                ResetEditor();
            }
        }
        catch (Exception)
        {
            Records.Clear();

            SelectedRecord =
                null;

            ResetEditor();

            ErrorMessage =
                "Không thể tải thông tin giấy tờ định danh.";
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
        SelectedRecord =
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
            && EditingRecordId.HasValue
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
            EmployeeIdentificationType type =
                SelectedType?.Value
                ?? (EmployeeIdentificationType)0;

            EmployeeIdentificationRecordOperationResult result =
                await _recordService
                    .SaveRecordAsync(
                        new SaveEmployeeIdentificationRecordRequest(
                            _employeeId,
                            EditingRecordId,
                            type,
                            DocumentNumber ?? string.Empty,
                            ToDateOnly(IssueDate),
                            ToDateOnly(ExpiryDate),
                            IssuingAuthority,
                            PlaceOfIssue,
                            IssuingCountry));

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể lưu giấy tờ định danh.";

                return;
            }

            await RefreshRecordsAsync();

            SelectedRecord =
                result.RecordId.HasValue
                    ? Records.FirstOrDefault(
                        record =>
                            record.Id ==
                            result.RecordId.Value)
                    : null;

            SuccessMessage =
                "Đã lưu giấy tờ định danh.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể lưu giấy tờ định danh.";
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
            || !EditingRecordId.HasValue)
        {
            return;
        }

        string documentNumber =
            string.IsNullOrWhiteSpace(
                DocumentNumber)
                ? "giấy tờ này"
                : DocumentNumber.Trim();

        bool confirmed =
            _confirmationDialogService.Confirm(
                "Xác nhận xóa giấy tờ",
                $"Bạn có muốn xóa giấy tờ “{documentNumber}” không?");

        if (!confirmed)
        {
            return;
        }

        Guid recordId =
            EditingRecordId.Value;

        IsBusy =
            true;

        ErrorMessage =
            null;

        SuccessMessage =
            null;

        NotifyCommands();

        try
        {
            EmployeeIdentificationRecordOperationResult result =
                await _recordService
                    .DeleteRecordAsync(
                        _employeeId,
                        recordId);

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể xóa giấy tờ định danh.";

                return;
            }

            await RefreshRecordsAsync();

            SelectedRecord =
                null;

            ResetEditor();

            SuccessMessage =
                "Đã xóa giấy tờ định danh.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể xóa giấy tờ định danh.";
        }
        finally
        {
            IsBusy =
                false;

            NotifyCommands();
        }
    }

    private async Task RefreshRecordsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EmployeeIdentificationRecordDetails> records =
            await _recordService
                .GetRecordsAsync(
                    _employeeId,
                    cancellationToken);

        Records.Clear();

        foreach (EmployeeIdentificationRecordDetails record
                 in records)
        {
            Records.Add(
                record);
        }
    }

    private void ResetEditor()
    {
        EditingRecordId =
            null;

        SelectedType =
            TypeOptions[0];

        DocumentNumber =
            null;

        IssueDate =
            null;

        ExpiryDate =
            null;

        IssuingAuthority =
            null;

        PlaceOfIssue =
            null;

        IssuingCountry =
            null;

        NotifyCommands();
    }

    partial void OnSelectedRecordChanged(
        EmployeeIdentificationRecordDetails? value)
    {
        ErrorMessage =
            null;

        SuccessMessage =
            null;

        if (value is null)
        {
            return;
        }

        EditingRecordId =
            value.Id;

        SelectedType =
            TypeOptions.Single(
                option =>
                    option.Value ==
                    value.Type);

        DocumentNumber =
            value.DocumentNumber;

        IssueDate =
            ToDateTime(
                value.IssueDate);

        ExpiryDate =
            ToDateTime(
                value.ExpiryDate);

        IssuingAuthority =
            value.IssuingAuthority;

        PlaceOfIssue =
            value.PlaceOfIssue;

        IssuingCountry =
            value.IssuingCountry;

        NotifyCommands();
    }

    partial void OnEditingRecordIdChanged(
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

    private static DateOnly? ToDateOnly(
        DateTime? value)
    {
        return value.HasValue
            ? DateOnly.FromDateTime(
                value.Value)
            : null;
    }

    private static DateTime? ToDateTime(
        DateOnly? value)
    {
        return value.HasValue
            ? value.Value.ToDateTime(
                TimeOnly.MinValue)
            : null;
    }
}
