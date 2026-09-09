using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authorization;
using HrManagement.Domain.Authentication.Accounts;

namespace HrManagement.Desktop.ViewModels;

public sealed class EditAccountProfileViewModel
    : ObservableObject
{
    private readonly IAccountProfileUpdateService
        _updateService;

    private readonly IAccountManagementQueryService
        _queryService;

    private Guid _accountId;

    private string _username =
        string.Empty;

    private string _kindText =
        string.Empty;

    private string _displayName =
        string.Empty;

    private IReadOnlyList<AccountEmployeeLinkOption>
        _employeeOptions =
            Array.Empty<AccountEmployeeLinkOption>();

    private AccountEmployeeLinkOption?
        _selectedEmployeeOption;

    private string? _errorMessage;

    private bool _isBusy;

    private bool _isReady;

    public EditAccountProfileViewModel(
        IAccountProfileUpdateService updateService,
        IAccountManagementQueryService queryService)
    {
        _updateService =
            updateService;

        _queryService =
            queryService;

        SaveCommand =
            new AsyncRelayCommand(
                SaveAsync,
                CanSave);
    }

    public event EventHandler?
        AccountUpdated;

    public string Username
    {
        get =>
            _username;

        private set =>
            SetProperty(
                ref _username,
                value);
    }

    public string KindText
    {
        get =>
            _kindText;

        private set =>
            SetProperty(
                ref _kindText,
                value);
    }

    public string DisplayName
    {
        get =>
            _displayName;

        set
        {
            if (SetProperty(
                    ref _displayName,
                    value))
            {
                ErrorMessage =
                    null;
            }
        }
    }

    public IReadOnlyList<AccountEmployeeLinkOption>
        EmployeeOptions
    {
        get =>
            _employeeOptions;

        private set =>
            SetProperty(
                ref _employeeOptions,
                value);
    }

    public AccountEmployeeLinkOption?
        SelectedEmployeeOption
    {
        get =>
            _selectedEmployeeOption;

        set
        {
            if (SetProperty(
                    ref _selectedEmployeeOption,
                    value))
            {
                ErrorMessage =
                    null;
            }
        }
    }

    public string? ErrorMessage
    {
        get =>
            _errorMessage;

        private set =>
            SetProperty(
                ref _errorMessage,
                value);
    }

    public bool IsBusy
    {
        get =>
            _isBusy;

        private set
        {
            if (SetProperty(
                    ref _isBusy,
                    value))
            {
                OnPropertyChanged(
                    nameof(CanSubmit));

                SaveCommand
                    .NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsReady
    {
        get =>
            _isReady;

        private set
        {
            if (SetProperty(
                    ref _isReady,
                    value))
            {
                OnPropertyChanged(
                    nameof(CanSubmit));

                SaveCommand
                    .NotifyCanExecuteChanged();
            }
        }
    }

    public bool CanSubmit =>
        IsReady
        && !IsBusy;

    public IAsyncRelayCommand SaveCommand
    {
        get;
    }

    public async Task LoadAsync(
        Guid accountId)
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage =
            null;

        IsReady =
            false;

        if (accountId == Guid.Empty)
        {
            ErrorMessage =
                "Tài khoản không hợp lệ.";

            return;
        }

        IsBusy =
            true;

        try
        {
            AccountManagementSnapshot snapshot =
                await _queryService
                    .GetAsync();

            AccountManagementAccountItem? account =
                snapshot.Accounts
                    .FirstOrDefault(
                        item =>
                            item.AccountId ==
                            accountId);

            if (account is null)
            {
                ErrorMessage =
                    "Không tìm thấy tài khoản.";

                return;
            }

            _accountId =
                account.AccountId;

            Username =
                account.Username;

            KindText =
                account.Kind ==
                    UserAccountKind.Owner
                    ? "Owner"
                    : "Standard";

            DisplayName =
                account.DisplayName;

            HashSet<Guid> linkedByOtherAccounts =
                snapshot.Accounts
                    .Where(
                        item =>
                            item.AccountId !=
                                accountId
                            && item.EmployeeId
                                .HasValue)
                    .Select(
                        item =>
                            item.EmployeeId!.Value)
                    .ToHashSet();

            var options =
                new List<AccountEmployeeLinkOption>
                {
                    new(
                        null,
                        "Không liên kết nhân viên")
                };

            options.AddRange(
                snapshot.Employees
                    .Where(
                        employee =>
                            !linkedByOtherAccounts
                                .Contains(
                                    employee.EmployeeId))
                    .Select(
                        employee =>
                            new AccountEmployeeLinkOption(
                                employee.EmployeeId,
                                $"{employee.EmployeeCode} - {employee.FullName}")));

            EmployeeOptions =
                options;

            if (account.EmployeeId.HasValue)
            {
                SelectedEmployeeOption =
                    EmployeeOptions
                        .FirstOrDefault(
                            option =>
                                option.EmployeeId ==
                                account.EmployeeId);

                if (SelectedEmployeeOption is null)
                {
                    ErrorMessage =
                        "Không thể xác định nhân viên đang liên kết.";

                    return;
                }
            }
            else
            {
                SelectedEmployeeOption =
                    EmployeeOptions[0];
            }

            IsReady =
                true;
        }
        catch (AuthorizationDeniedException)
        {
            ErrorMessage =
                "Bạn không có quyền xem dữ liệu quản trị tài khoản.";
        }
        catch (OperationCanceledException)
        {
            ErrorMessage =
                "Thao tác tải tài khoản đã bị hủy.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể tải thông tin tài khoản.";
        }
        finally
        {
            IsBusy =
                false;
        }
    }

    private bool CanSave()
    {
        return IsReady
            && !IsBusy;
    }

    private async Task SaveAsync()
    {
        ErrorMessage =
            null;

        if (!IsReady
            || _accountId == Guid.Empty)
        {
            ErrorMessage =
                "Tài khoản chưa sẵn sàng để cập nhật.";

            return;
        }

        try
        {
            IsBusy =
                true;

            UpdateAccountProfileResult result =
                await _updateService
                    .UpdateAsync(
                        new UpdateAccountProfileRequest(
                            _accountId,
                            DisplayName,
                            SelectedEmployeeOption
                                ?.EmployeeId));

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể cập nhật tài khoản.";

                return;
            }

            AccountUpdated?.Invoke(
                this,
                EventArgs.Empty);
        }
        catch (AuthorizationDeniedException)
        {
            ErrorMessage =
                "Bạn không có quyền sửa tài khoản.";
        }
        catch (OperationCanceledException)
        {
            ErrorMessage =
                "Thao tác cập nhật tài khoản đã bị hủy.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Đã xảy ra lỗi khi cập nhật tài khoản. Vui lòng thử lại.";
        }
        finally
        {
            IsBusy =
                false;
        }
    }
}
