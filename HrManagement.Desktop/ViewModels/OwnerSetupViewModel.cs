using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Authentication.Bootstrap;

namespace HrManagement.Desktop.ViewModels;

public sealed record OwnerSetupPasswords(
    string Password,
    string ConfirmPassword);

public sealed class OwnerSetupViewModel
    : ObservableObject
{
    private readonly IInitialOwnerBootstrapService
        _bootstrapService;

    private string _username =
        string.Empty;

    private string _displayName =
        string.Empty;

    private string? _errorMessage;

    private string? _recoveryCode;

    private bool _isRecoveryCodeReady;

    private bool _isBusy;

    public OwnerSetupViewModel(
        IInitialOwnerBootstrapService bootstrapService)
    {
        _bootstrapService =
            bootstrapService;

        CreateOwnerCommand =
            new AsyncRelayCommand<OwnerSetupPasswords?>(
                CreateOwnerAsync,
                CanCreateOwner);

        ConfirmRecoveryCodeSavedCommand =
            new RelayCommand(
                ConfirmRecoveryCodeSaved,
                CanConfirmRecoveryCodeSaved);
    }

    public event EventHandler? OwnerCreated;

    public event EventHandler? RecoveryCodeReady;

    public string? RecoveryCode
    {
        get =>
            _recoveryCode;

        private set =>
            SetProperty(
                ref _recoveryCode,
                value);
    }

    public bool IsRecoveryCodeReady
    {
        get =>
            _isRecoveryCodeReady;

        private set
        {
            if (SetProperty(
                    ref _isRecoveryCodeReady,
                    value))
            {
                CreateOwnerCommand
                    .NotifyCanExecuteChanged();

                ConfirmRecoveryCodeSavedCommand
                    .NotifyCanExecuteChanged();
            }
        }
    }

    public IRelayCommand
        ConfirmRecoveryCodeSavedCommand
    {
        get;
    }

    public string Username
    {
        get =>
            _username;

        set
        {
            if (SetProperty(
                    ref _username,
                    value))
            {
                ErrorMessage =
                    null;

                CreateOwnerCommand
                    .NotifyCanExecuteChanged();
            }
        }
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

                CreateOwnerCommand
                    .NotifyCanExecuteChanged();
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
                CreateOwnerCommand
                    .NotifyCanExecuteChanged();

                ConfirmRecoveryCodeSavedCommand
                    .NotifyCanExecuteChanged();
            }
        }
    }

    public IAsyncRelayCommand<OwnerSetupPasswords?>
        CreateOwnerCommand
    {
        get;
    }

    private bool CanCreateOwner(
    OwnerSetupPasswords? passwords)
    {
        return !IsBusy
            && !IsRecoveryCodeReady
            && passwords is not null
            && !string.IsNullOrWhiteSpace(
                Username)
            && !string.IsNullOrWhiteSpace(
                DisplayName);
    }

    private async Task CreateOwnerAsync(
        OwnerSetupPasswords? passwords)
    {
        ErrorMessage =
            null;

        if (passwords is null
            || string.IsNullOrEmpty(
                passwords.Password))
        {
            ErrorMessage =
                "Vui lòng nhập mật khẩu.";

            return;
        }

        if (!string.Equals(
                passwords.Password,
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

            OwnerBootstrapResult result =
                await _bootstrapService
                    .CreateInitialOwnerAsync(
                        Username,
                        DisplayName,
                        passwords.Password);

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể tạo tài khoản Chủ doanh nghiệp.";

                return;
            }

            if (string.IsNullOrWhiteSpace(
                result.RecoveryCode))
            {
                ErrorMessage =
                    "Không thể tạo mã khôi phục cho tài khoản Owner.";

                return;
            }

            RecoveryCode =
                result.RecoveryCode;

            IsRecoveryCodeReady =
                true;

            RecoveryCodeReady?.Invoke(
                this,
                EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            ErrorMessage =
                "Thao tác thiết lập đã bị hủy.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Đã xảy ra lỗi khi thiết lập tài khoản. Vui lòng thử lại.";
        }
        finally
        {
            IsBusy =
                false;
        }
    }

    private bool CanConfirmRecoveryCodeSaved()
    {
        return IsRecoveryCodeReady
            && !IsBusy
            && !string.IsNullOrWhiteSpace(
                RecoveryCode);
    }

    private void ConfirmRecoveryCodeSaved()
    {
        if (!CanConfirmRecoveryCodeSaved())
        {
            return;
        }

        OwnerCreated?.Invoke(
            this,
            EventArgs.Empty);
    }
}
