using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Authentication.Credentials;

namespace HrManagement.Desktop.ViewModels;

public sealed record ChangePasswordPasswords(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword);

public sealed class ChangePasswordViewModel
    : ObservableObject
{
    private readonly IChangePasswordService
        _changePasswordService;

    private string? _errorMessage;

    private bool _isBusy;

    public ChangePasswordViewModel(
        IChangePasswordService changePasswordService)
    {
        _changePasswordService =
            changePasswordService;

        ChangePasswordCommand =
            new AsyncRelayCommand<
                ChangePasswordPasswords?>(
                    ChangePasswordAsync,
                    CanChangePassword);
    }

    public event EventHandler?
        PasswordChanged;

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
                ChangePasswordCommand
                    .NotifyCanExecuteChanged();

                OnPropertyChanged(
                    nameof(CanSubmit));
            }
        }
    }

    public bool CanSubmit =>
        !IsBusy;

    public IAsyncRelayCommand<
        ChangePasswordPasswords?>
        ChangePasswordCommand
    {
        get;
    }

    private bool CanChangePassword(
        ChangePasswordPasswords? passwords)
    {
        return !IsBusy;
    }

    private async Task ChangePasswordAsync(
        ChangePasswordPasswords? passwords)
    {
        ErrorMessage =
            null;

        if (passwords is null
            || string.IsNullOrEmpty(
                passwords.CurrentPassword))
        {
            ErrorMessage =
                "Vui lòng nhập mật khẩu hiện tại.";

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

            ChangePasswordResult result =
                await _changePasswordService
                    .ChangeAsync(
                        passwords.CurrentPassword,
                        passwords.NewPassword);

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể đổi mật khẩu.";

                return;
            }

            PasswordChanged?.Invoke(
                this,
                EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            ErrorMessage =
                "Thao tác đổi mật khẩu đã bị hủy.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Đã xảy ra lỗi khi đổi mật khẩu. Vui lòng thử lại.";
        }
        finally
        {
            IsBusy =
                false;
        }
    }
}
