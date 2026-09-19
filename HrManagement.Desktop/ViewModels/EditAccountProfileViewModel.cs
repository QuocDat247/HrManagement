using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authentication;
using HrManagement.Application.Authentication.Credentials;
using HrManagement.Application.Authorization;
using HrManagement.Domain.Authentication.Accounts;

namespace HrManagement.Desktop.ViewModels;

public sealed record ResetAccountPasswordPasswords(
    string NewPassword,
    string ConfirmPassword);

public sealed class EditAccountProfileViewModel
    : ObservableObject
{
    private readonly IAccountProfileUpdateService
        _updateService;

    private readonly IAccountManagementQueryService
        _queryService;

    private readonly IAccountPasswordResetService
        _passwordResetService;

    private readonly ICurrentUserContext
        _currentUserContext;

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

    private string? _successMessage;

    private bool _isPasswordResetAvailable;

    private bool _isBusy;

    private bool _isReady;

    public EditAccountProfileViewModel(
    IAccountProfileUpdateService updateService,
    IAccountManagementQueryService queryService,
    IAccountPasswordResetService passwordResetService,
    ICurrentUserContext currentUserContext)
    {
        _updateService =
            updateService;

        _queryService =
            queryService;

        _passwordResetService =
            passwordResetService;

        _currentUserContext =
            currentUserContext;

        SaveCommand =
            new AsyncRelayCommand(
                SaveAsync,
                CanSave);

        ResetPasswordCommand =
            new AsyncRelayCommand<
                ResetAccountPasswordPasswords?>(
                    ResetPasswordAsync,
                    CanResetPassword);
    }

    public event EventHandler?
        AccountUpdated;

    public event EventHandler?
        PasswordReset;

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

    public string? SuccessMessage
    {
        get =>
            _successMessage;

        private set =>
            SetProperty(
                ref _successMessage,
                value);
    }

    public bool IsPasswordResetAvailable
    {
        get =>
            _isPasswordResetAvailable;

        private set
        {
            if (SetProperty(
                    ref _isPasswordResetAvailable,
                    value))
            {
                OnPropertyChanged(
                    nameof(CanResetPasswordSubmit));

                ResetPasswordCommand
                    .NotifyCanExecuteChanged();
            }
        }
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

                OnPropertyChanged(
                    nameof(CanResetPasswordSubmit));

                ResetPasswordCommand
                    .NotifyCanExecuteChanged();

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

                OnPropertyChanged(
                    nameof(CanResetPasswordSubmit));

                ResetPasswordCommand
                    .NotifyCanExecuteChanged();

                SaveCommand
                    .NotifyCanExecuteChanged();
            }
        }
    }

    public bool CanSubmit =>
        IsReady
        && !IsBusy;

    public bool CanResetPasswordSubmit =>
    IsReady
    && IsPasswordResetAvailable
    && !IsBusy;

    public IAsyncRelayCommand SaveCommand
    {
        get;
    }

    public IAsyncRelayCommand<
    ResetAccountPasswordPasswords?>
    ResetPasswordCommand
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

        SuccessMessage =
            null;

        IsPasswordResetAvailable =
            false;

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

            AuthenticatedUser? currentUser =
                _currentUserContext.CurrentUser;

            bool currentUserIsOwner =
                currentUser is not null
                && Guid.TryParse(
                    currentUser.UserId,
                    out Guid currentAccountId)
                && snapshot.Accounts.Any(
                    item =>
                        item.AccountId ==
                            currentAccountId
                        && item.Kind ==
                            UserAccountKind.Owner
                        && item.IsActive);

            IsPasswordResetAvailable =
                currentUserIsOwner
                && account.Kind ==
                    UserAccountKind.Standard;

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

    private bool CanResetPassword(
    ResetAccountPasswordPasswords? passwords)
    {
        return CanResetPasswordSubmit;
    }

    private async Task ResetPasswordAsync(
        ResetAccountPasswordPasswords? passwords)
    {
        ErrorMessage =
            null;

        SuccessMessage =
            null;

        if (!IsPasswordResetAvailable
            || _accountId == Guid.Empty)
        {
            ErrorMessage =
                "Chỉ Owner mới có thể đặt lại mật khẩu cho tài khoản Standard.";

            return;
        }

        if (passwords is null
            || string.IsNullOrEmpty(
                passwords.NewPassword))
        {
            ErrorMessage =
                "Vui lòng nhập mật khẩu tạm mới.";

            return;
        }

        if (!string.Equals(
                passwords.NewPassword,
                passwords.ConfirmPassword,
                StringComparison.Ordinal))
        {
            ErrorMessage =
                "Mật khẩu xác nhận không khớp.";

            return;
        }

        try
        {
            IsBusy =
                true;

            ResetAccountPasswordResult result =
                await _passwordResetService
                    .ResetAsync(
                        new ResetAccountPasswordRequest(
                            _accountId,
                            passwords.NewPassword));

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể đặt lại mật khẩu.";

                return;
            }

            SuccessMessage =
                "Đã đặt lại mật khẩu tạm. "
                + "Người dùng sẽ phải đổi mật khẩu khi đăng nhập lần tiếp theo.";

            PasswordReset?.Invoke(
                this,
                EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            ErrorMessage =
                "Thao tác đặt lại mật khẩu đã bị hủy.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Đã xảy ra lỗi khi đặt lại mật khẩu. Vui lòng thử lại.";
        }
        finally
        {
            IsBusy =
                false;
        }
    }
}
