using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authorization;
using HrManagement.Application.Authorization.Roles;
using HrManagement.Domain.Authentication.Accounts;

namespace HrManagement.Desktop.ViewModels;

public sealed class AccountRoleSelectionItem
    : ObservableObject
{
    private bool _isSelected;

    public AccountRoleSelectionItem(
        Guid roleId,
        string name,
        string? description,
        bool isActive,
        bool isSelected)
    {
        RoleId =
            roleId;

        Name =
            name;

        Description =
            description;

        IsActive =
            isActive;

        _isSelected =
            isSelected;
    }

    public Guid RoleId
    {
        get;
    }

    public string Name
    {
        get;
    }

    public string? Description
    {
        get;
    }

    public bool IsActive
    {
        get;
    }

    public string StatusText =>
        IsActive
            ? "Đang sử dụng"
            : "Ngừng sử dụng";

    public bool IsSelected
    {
        get =>
            _isSelected;

        set =>
            SetProperty(
                ref _isSelected,
                value);
    }
}

public sealed class AccountRoleEditorViewModel
    : ObservableObject
{
    private readonly IAccountManagementQueryService
        _queryService;

    private readonly IUserAccountRoleAssignmentService
        _assignmentService;

    private Guid _accountId;

    private string _username =
        string.Empty;

    private string _displayName =
        string.Empty;

    private string _accountStatusText =
        string.Empty;

    private IReadOnlyList<AccountRoleSelectionItem>
        _roles =
            Array.Empty<AccountRoleSelectionItem>();

    private string? _errorMessage;

    private bool _isBusy;

    private bool _isReady;

    public AccountRoleEditorViewModel(
        IAccountManagementQueryService queryService,
        IUserAccountRoleAssignmentService assignmentService)
    {
        _queryService =
            queryService;

        _assignmentService =
            assignmentService;

        SaveCommand =
            new AsyncRelayCommand(
                SaveAsync,
                CanSave);
    }

    public event EventHandler?
        RolesSaved;

    public string Username
    {
        get =>
            _username;

        private set =>
            SetProperty(
                ref _username,
                value);
    }

    public string DisplayName
    {
        get =>
            _displayName;

        private set =>
            SetProperty(
                ref _displayName,
                value);
    }

    public string AccountStatusText
    {
        get =>
            _accountStatusText;

        private set =>
            SetProperty(
                ref _accountStatusText,
                value);
    }

    public IReadOnlyList<AccountRoleSelectionItem>
        Roles
    {
        get =>
            _roles;

        private set =>
            SetProperty(
                ref _roles,
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

        _accountId =
            Guid.Empty;

        Username =
            string.Empty;

        DisplayName =
            string.Empty;

        AccountStatusText =
            string.Empty;

        Roles =
            Array.Empty<AccountRoleSelectionItem>();

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
                    .SingleOrDefault(
                        item =>
                            item.AccountId ==
                            accountId);

            if (account is null)
            {
                ErrorMessage =
                    "Không tìm thấy tài khoản.";

                return;
            }

            if (account.Kind ==
                UserAccountKind.Owner)
            {
                ErrorMessage =
                    "Owner không sử dụng vai trò phân quyền.";

                return;
            }

            _accountId =
                account.AccountId;

            Username =
                account.Username;

            DisplayName =
                account.DisplayName;

            AccountStatusText =
                account.IsActive
                    ? "Đang hoạt động"
                    : "Đã vô hiệu hóa";

            HashSet<Guid> assignedRoleIds =
                account.Roles
                    .Select(
                        role =>
                            role.RoleId)
                    .ToHashSet();

            Roles =
                snapshot.Roles
                    .Select(
                        role =>
                            new AccountRoleSelectionItem(
                                role.RoleId,
                                role.Name,
                                role.Description,
                                role.IsActive,
                                assignedRoleIds.Contains(
                                    role.RoleId)))
                    .ToArray();

            IsReady =
                true;
        }
        catch (AuthorizationDeniedException)
        {
            ErrorMessage =
                "Bạn không có quyền xem dữ liệu tài khoản.";
        }
        catch (OperationCanceledException)
        {
            ErrorMessage =
                "Thao tác tải vai trò đã bị hủy.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể tải vai trò của tài khoản.";
        }
        finally
        {
            IsBusy =
                false;
        }
    }

    private bool CanSave()
    {
        return CanSubmit;
    }

    private async Task SaveAsync()
    {
        if (!CanSubmit
            || _accountId == Guid.Empty)
        {
            return;
        }

        ErrorMessage =
            null;

        try
        {
            IsBusy =
                true;

            Guid[] roleIds =
                Roles
                    .Where(
                        role =>
                            role.IsSelected)
                    .Select(
                        role =>
                            role.RoleId)
                    .OrderBy(
                        roleId =>
                            roleId)
                    .ToArray();

            AccountRoleAssignmentResult result =
                await _assignmentService
                    .ReplaceAsync(
                        new ReplaceAccountRolesRequest(
                            _accountId,
                            roleIds));

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể cập nhật vai trò của tài khoản.";

                return;
            }

            RolesSaved?.Invoke(
                this,
                EventArgs.Empty);
        }
        catch (AuthorizationDeniedException)
        {
            ErrorMessage =
                "Bạn không có quyền thay đổi vai trò của tài khoản.";
        }
        catch (OperationCanceledException)
        {
            ErrorMessage =
                "Thao tác cập nhật vai trò đã bị hủy.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Đã xảy ra lỗi khi cập nhật vai trò của tài khoản.";
        }
        finally
        {
            IsBusy =
                false;
        }
    }
}
