using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Authentication;

namespace HrManagement.Desktop.ViewModels;

public sealed class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationService _authenticationService;

    private string _username = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;

    public LoginViewModel(
        IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;

        LoginCommand = new AsyncRelayCommand<string?>(
            LoginAsync,
            CanLogin);
    }

    public event EventHandler? LoginSucceeded;

    public string Username
    {
        get => _username;

        set
        {
            if (SetProperty(ref _username, value))
            {
                ErrorMessage = null;
                LoginCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;

        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                LoginCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public IAsyncRelayCommand<string?> LoginCommand { get; }

    private bool CanLogin(string? password)
    {
        return !IsBusy
            && !string.IsNullOrWhiteSpace(Username);
    }

    private async Task LoginAsync(string? password)
    {
        ErrorMessage = null;

        if (string.IsNullOrEmpty(password))
        {
            ErrorMessage = "Vui lòng nhập mật khẩu.";
            return;
        }

        try
        {
            IsBusy = true;

            AuthenticationResult result =
                await _authenticationService.LoginAsync(
                    Username,
                    password);

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể đăng nhập.";

                return;
            }

            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Thao tác đăng nhập đã bị hủy.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Đã xảy ra lỗi khi đăng nhập. Vui lòng thử lại.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}