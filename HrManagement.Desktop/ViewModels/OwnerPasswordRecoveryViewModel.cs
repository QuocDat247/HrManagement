using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Authentication.Recovery;

namespace HrManagement.Desktop.ViewModels;

public sealed record OwnerPasswordRecoveryPasswords(
    string RecoveryCode,
    string NewPassword,
    string ConfirmPassword);

public sealed class OwnerPasswordRecoveryViewModel
    : ObservableObject
{
    private readonly IOwnerPasswordRecoveryService
        _recoveryService;

    private string _username =
        string.Empty;

    private string _newRecoveryCode =
        string.Empty;

    private string? _errorMessage;

    private bool _isBusy;

    private bool _isRecoveryCompleted;

    private bool _isCompletionAcknowledged;

    public OwnerPasswordRecoveryViewModel(
        IOwnerPasswordRecoveryService recoveryService)
    {
        _recoveryService =
            recoveryService;

        RecoverCommand =
            new AsyncRelayCommand<
                OwnerPasswordRecoveryPasswords?>(
                    RecoverAsync,
                    CanRecover);

        ConfirmRecoveryCodeSavedCommand =
            new RelayCommand(
                ConfirmRecoveryCodeSaved,
                CanConfirmRecoveryCodeSaved);
    }

    public event EventHandler?
        RecoverySucceeded;

    public event EventHandler?
        RecoveryFinished;

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

                RecoverCommand
                    .NotifyCanExecuteChanged();
            }
        }
    }

    public string NewRecoveryCode
    {
        get =>
            _newRecoveryCode;

        private set =>
            SetProperty(
                ref _newRecoveryCode,
                value);
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
                RecoverCommand
                    .NotifyCanExecuteChanged();

                ConfirmRecoveryCodeSavedCommand
                    .NotifyCanExecuteChanged();

                OnPropertyChanged(
                    nameof(CanSubmit));
            }
        }
    }

    public bool IsRecoveryCompleted
    {
        get =>
            _isRecoveryCompleted;

        private set
        {
            if (SetProperty(
                    ref _isRecoveryCompleted,
                    value))
            {
                RecoverCommand
                    .NotifyCanExecuteChanged();

                ConfirmRecoveryCodeSavedCommand
                    .NotifyCanExecuteChanged();

                OnPropertyChanged(
                    nameof(CanSubmit));
            }
        }
    }

    public bool IsCompletionAcknowledged
    {
        get =>
            _isCompletionAcknowledged;

        private set =>
            SetProperty(
                ref _isCompletionAcknowledged,
                value);
    }

    public bool CanSubmit =>
        !IsBusy
        && !IsRecoveryCompleted;

    public IAsyncRelayCommand<
        OwnerPasswordRecoveryPasswords?>
        RecoverCommand
    {
        get;
    }

    public IRelayCommand
        ConfirmRecoveryCodeSavedCommand
    {
        get;
    }

    public void ClearSensitiveState()
    {
        NewRecoveryCode =
            string.Empty;
    }

    private bool CanRecover(
        OwnerPasswordRecoveryPasswords? passwords)
    {
        return !IsBusy
            && !IsRecoveryCompleted
            && !string.IsNullOrWhiteSpace(
                Username);
    }

    private async Task RecoverAsync(
        OwnerPasswordRecoveryPasswords? passwords)
    {
        ErrorMessage =
            null;

        if (passwords is null
            || string.IsNullOrWhiteSpace(
                passwords.RecoveryCode))
        {
            ErrorMessage =
                "Vui lòng nhập mã khôi phục.";

            return;
        }

        if (string.IsNullOrEmpty(
                passwords.NewPassword))
        {
            ErrorMessage =
                "Vui lòng nhập mật khẩu mới.";

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

            OwnerPasswordRecoveryResult result =
                await _recoveryService
                    .RecoverAsync(
                        new OwnerPasswordRecoveryRequest(
                            Username,
                            passwords.RecoveryCode,
                            passwords.NewPassword));

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể khôi phục tài khoản Owner.";

                return;
            }

            if (string.IsNullOrWhiteSpace(
                    result.NewRecoveryCode))
            {
                ErrorMessage =
                    "Không thể tạo mã khôi phục mới.";

                return;
            }

            NewRecoveryCode =
                result.NewRecoveryCode;

            IsRecoveryCompleted =
                true;

            RecoverySucceeded?.Invoke(
                this,
                EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            ErrorMessage =
                "Thao tác khôi phục đã bị hủy.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Đã xảy ra lỗi khi khôi phục tài khoản. Vui lòng thử lại.";
        }
        finally
        {
            IsBusy =
                false;
        }
    }

    private bool CanConfirmRecoveryCodeSaved()
    {
        return IsRecoveryCompleted
            && !IsBusy
            && !string.IsNullOrWhiteSpace(
                NewRecoveryCode);
    }

    private void ConfirmRecoveryCodeSaved()
    {
        if (!CanConfirmRecoveryCodeSaved())
        {
            return;
        }

        IsCompletionAcknowledged =
            true;

        RecoveryFinished?.Invoke(
            this,
            EventArgs.Empty);
    }
}
