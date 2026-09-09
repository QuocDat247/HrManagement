using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authorization;

namespace HrManagement.Desktop.ViewModels;

public sealed record CreateStandardAccountPasswords(
    string Password,
    string ConfirmPassword);

public sealed record AccountEmployeeLinkOption(
    Guid? EmployeeId,
    string DisplayText);

public sealed class CreateStandardAccountViewModel
    : ObservableObject
{
    private readonly IStandardAccountCreationService
        _creationService;

    private readonly IAccountManagementQueryService
        _queryService;

    private string _username =
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

    public CreateStandardAccountViewModel(
        IStandardAccountCreationService creationService,
        IAccountManagementQueryService queryService)
    {
        _creationService =
            creationService;

        _queryService =
            queryService;

        CreateAccountCommand =
            new AsyncRelayCommand<
                CreateStandardAccountPasswords?>(
                    CreateAccountAsync,
                    CanCreateAccount);
    }

    public event EventHandler?
        AccountCreated;

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

                CreateAccountCommand
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

                CreateAccountCommand
                    .NotifyCanExecuteChanged();
            }
        }
    }

    public bool CanSubmit =>
        IsReady
        && !IsBusy;

    public IAsyncRelayCommand<
        CreateStandardAccountPasswords?>
        CreateAccountCommand
    {
        get;
    }

    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage =
            null;

        IsReady =
            false;

        IsBusy =
            true;

        try
        {
            AccountManagementSnapshot snapshot =
                await _queryService
                    .GetAsync();

            HashSet<Guid> linkedEmployeeIds =
                snapshot.Accounts
                    .Where(
                        account =>
                            account.EmployeeId.HasValue)
                    .Select(
                        account =>
                            account.EmployeeId!.Value)
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
                            !linkedEmployeeIds.Contains(
                                employee.EmployeeId))
                    .Select(
                        employee =>
                            new AccountEmployeeLinkOption(
                                employee.EmployeeId,
                                $"{employee.EmployeeCode} - {employee.FullName}")));

            EmployeeOptions =
                options;

            SelectedEmployeeOption =
                EmployeeOptions[0];

            IsReady =
                true;
        }
        catch (AuthorizationDeniedException)
        {
            EmployeeOptions =
                Array.Empty<AccountEmployeeLinkOption>();

            SelectedEmployeeOption =
                null;

            ErrorMessage =
                "Bạn không có quyền xem dữ liệu quản trị tài khoản.";
        }
        catch (Exception)
        {
            EmployeeOptions =
                Array.Empty<AccountEmployeeLinkOption>();

            SelectedEmployeeOption =
                null;

            ErrorMessage =
                "Không thể tải danh sách nhân viên để liên kết.";
        }
        finally
        {
            IsBusy =
                false;
        }
    }

    private bool CanCreateAccount(
        CreateStandardAccountPasswords? passwords)
    {
        return IsReady
            && !IsBusy;
    }

    private async Task CreateAccountAsync(
        CreateStandardAccountPasswords? passwords)
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

            CreateStandardAccountResult result =
                await _creationService
                    .CreateAsync(
                        new CreateStandardAccountRequest(
                            Username,
                            DisplayName,
                            passwords.Password,
                            SelectedEmployeeOption
                                ?.EmployeeId));

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể tạo tài khoản.";

                return;
            }

            AccountCreated?.Invoke(
                this,
                EventArgs.Empty);
        }
        catch (AuthorizationDeniedException)
        {
            ErrorMessage =
                "Bạn không có quyền tạo tài khoản.";
        }
        catch (OperationCanceledException)
        {
            ErrorMessage =
                "Thao tác tạo tài khoản đã bị hủy.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Đã xảy ra lỗi khi tạo tài khoản. Vui lòng thử lại.";
        }
        finally
        {
            IsBusy =
                false;
        }
    }
}
