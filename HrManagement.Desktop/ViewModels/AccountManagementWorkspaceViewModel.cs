using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Desktop.Services.Accounts;
using HrManagement.Desktop.Services;
using HrManagement.Application.Authorization.Roles;
using HrManagement.Application.Authorization;
using HrManagement.Application.Authentication.Accounts;
using HrManagement.Domain.Authentication.Accounts;

namespace HrManagement.Desktop.ViewModels;

public sealed partial class AccountManagementWorkspaceViewModel
    : ObservableObject
{
    private readonly IAccountManagementQueryService
        _queryService;

    private readonly IAccountManagementDialogService
        _dialogService;

    private readonly IAccountActiveStateService
        _activeStateService;

    private readonly IRoleActiveStateService
        _roleActiveStateService;

    private readonly IUserConfirmationService
        _confirmationService;

    [ObservableProperty]
    private IReadOnlyList<AccountManagementAccountRow>
        accountRows =
            Array.Empty<AccountManagementAccountRow>();

    [ObservableProperty]
    private AccountManagementAccountRow?
        selectedAccountRow;

    [ObservableProperty]
    private IReadOnlyList<AccountManagementRoleItem>
        roles =
            Array.Empty<AccountManagementRoleItem>();

    [ObservableProperty]
    private AccountManagementRoleItem?
        selectedRole;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    public IAsyncRelayCommand RefreshCommand
    {
        get;
    }

    public IAsyncRelayCommand CreateAccountCommand
    {
        get;
    }

    public IAsyncRelayCommand EditAccountCommand
    {
        get;
    }

    public IAsyncRelayCommand DeactivateAccountCommand
    {
        get;
    }

    public IAsyncRelayCommand ReactivateAccountCommand
    {
        get;
    }

    public IAsyncRelayCommand CreateRoleCommand
    {
        get;
    }

    public IAsyncRelayCommand EditRoleCommand
    {
        get;
    }

    public IAsyncRelayCommand DeactivateRoleCommand
    {
        get;
    }

    public IAsyncRelayCommand ReactivateRoleCommand
    {
        get;
    }

    public IAsyncRelayCommand ManageRolePermissionsCommand
    {
        get;
    }

    public AccountManagementWorkspaceViewModel(
        IAccountManagementQueryService queryService,
        IAccountManagementDialogService dialogService,
        IAccountActiveStateService activeStateService,
        IUserConfirmationService confirmationService,
        IRoleActiveStateService roleActiveStateService)
    {
        _queryService =
            queryService;

        _dialogService =
            dialogService;

        _activeStateService =
            activeStateService;

        _confirmationService =
            confirmationService;

        _roleActiveStateService =
            roleActiveStateService;

        RefreshCommand =
            new AsyncRelayCommand(
                LoadAsync);

        CreateAccountCommand =
            new AsyncRelayCommand(
                CreateAccountAsync);

        EditAccountCommand =
            new AsyncRelayCommand(
                EditAccountAsync,
                CanEditAccount);

        DeactivateAccountCommand =
            new AsyncRelayCommand(
                DeactivateAccountAsync,
                CanDeactivateAccount);

        ReactivateAccountCommand =
            new AsyncRelayCommand(
                ReactivateAccountAsync,
                CanReactivateAccount);

        CreateRoleCommand =
            new AsyncRelayCommand(
                CreateRoleAsync,
                CanCreateRole);

        EditRoleCommand =
            new AsyncRelayCommand(
                EditRoleAsync,
                CanEditRole);

        DeactivateRoleCommand =
            new AsyncRelayCommand(
                DeactivateRoleAsync,
                CanDeactivateRole);

        ReactivateRoleCommand =
            new AsyncRelayCommand(
                ReactivateRoleAsync,
                CanReactivateRole);

        ManageRolePermissionsCommand =
            new AsyncRelayCommand(
                ManageRolePermissionsAsync,
                CanManageRolePermissions);
    }

    public async Task LoadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading =
            true;

        ErrorMessage =
            null;

        try
        {
            AccountManagementSnapshot snapshot =
                await _queryService
                    .GetAsync();

            AccountRows =
                snapshot.Accounts
                    .Select(
                        AccountManagementAccountRow
                            .FromItem)
                    .ToArray();

            Roles =
                snapshot.Roles
                    .ToArray();

            SelectedAccountRow =
                PreserveAccountSelection(
                    AccountRows,
                    SelectedAccountRow);

            SelectedRole =
                PreserveRoleSelection(
                    Roles,
                    SelectedRole);
        }
        catch (Exception)
        {
            AccountRows =
                Array.Empty<AccountManagementAccountRow>();

            Roles =
                Array.Empty<AccountManagementRoleItem>();

            SelectedAccountRow =
                null;

            SelectedRole =
                null;

            ErrorMessage =
                "Không thể tải dữ liệu tài khoản và phân quyền.";
        }
        finally
        {
            IsLoading =
                false;
        }
    }

    partial void OnSelectedAccountRowChanged(
        AccountManagementAccountRow? value)
    {
        EditAccountCommand
            .NotifyCanExecuteChanged();

        DeactivateAccountCommand
            .NotifyCanExecuteChanged();

        ReactivateAccountCommand
            .NotifyCanExecuteChanged();
    }

    partial void OnSelectedRoleChanged(
        AccountManagementRoleItem? value)
    {
        EditRoleCommand
            .NotifyCanExecuteChanged();

        DeactivateRoleCommand
            .NotifyCanExecuteChanged();

        ReactivateRoleCommand
            .NotifyCanExecuteChanged();

        ManageRolePermissionsCommand
            .NotifyCanExecuteChanged();
    }

    partial void OnIsLoadingChanged(
        bool value)
    {
        EditAccountCommand
            .NotifyCanExecuteChanged();

        DeactivateAccountCommand
            .NotifyCanExecuteChanged();

        ReactivateAccountCommand
            .NotifyCanExecuteChanged();

        CreateRoleCommand
            .NotifyCanExecuteChanged();

        EditRoleCommand
            .NotifyCanExecuteChanged();

        DeactivateRoleCommand
            .NotifyCanExecuteChanged();

        ReactivateRoleCommand
            .NotifyCanExecuteChanged();

        ManageRolePermissionsCommand
            .NotifyCanExecuteChanged();
    }

    private async Task CreateAccountAsync()
    {
        if (IsLoading)
        {
            return;
        }

        ErrorMessage =
            null;

        try
        {
            bool created =
                _dialogService
                    .ShowCreateAccountDialog();

            if (created)
            {
                await LoadAsync();
            }
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể mở màn hình tạo tài khoản.";
        }
    }

    private bool CanEditAccount()
    {
        return !IsLoading
            && SelectedAccountRow is not null;
    }

    private async Task EditAccountAsync()
    {
        AccountManagementAccountRow? selected =
            SelectedAccountRow;

        if (selected is null
            || IsLoading)
        {
            return;
        }

        ErrorMessage =
            null;

        try
        {
            bool updated =
                _dialogService
                    .ShowEditAccountDialog(
                        selected.AccountId);

            if (updated)
            {
                await LoadAsync();
            }
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể mở màn hình sửa tài khoản.";
        }
    }

    private bool CanDeactivateAccount()
    {
        return !IsLoading
            && SelectedAccountRow is
            {
                IsActive: true
            };
    }

    private bool CanReactivateAccount()
    {
        return !IsLoading
            && SelectedAccountRow is
            {
                IsActive: false
            };
    }

    private Task DeactivateAccountAsync()
    {
        return SetSelectedAccountActiveStateAsync(
            isActive:
                false);
    }

    private Task ReactivateAccountAsync()
    {
        return SetSelectedAccountActiveStateAsync(
            isActive:
                true);
    }

    private async Task SetSelectedAccountActiveStateAsync(
        bool isActive)
    {
        AccountManagementAccountRow? selected =
            SelectedAccountRow;

        if (selected is null
            || IsLoading
            || selected.IsActive ==
                isActive)
        {
            return;
        }

        ErrorMessage =
            null;

        string title =
            isActive
                ? "Kích hoạt lại tài khoản"
                : "Vô hiệu hóa tài khoản";

        string message =
            isActive
                ? $"Kích hoạt lại tài khoản \"{selected.Username}\"?\n\n"
                    + "Tài khoản sẽ có thể đăng nhập lại."
                : $"Vô hiệu hóa tài khoản \"{selected.Username}\"?\n\n"
                    + "Tài khoản sẽ không thể đăng nhập "
                    + "cho đến khi được kích hoạt lại.";

        if (!_confirmationService
                .Confirm(
                    title,
                    message))
        {
            return;
        }

        try
        {
            SetAccountActiveStateResult result =
                await _activeStateService
                    .SetAsync(
                        new SetAccountActiveStateRequest(
                            selected.AccountId,
                            isActive));

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể thay đổi trạng thái tài khoản.";

                return;
            }

            await LoadAsync();
        }
        catch (AuthorizationDeniedException)
        {
            ErrorMessage =
                "Bạn không có quyền thay đổi trạng thái tài khoản.";
        }
        catch (OperationCanceledException)
        {
            ErrorMessage =
                "Thao tác thay đổi trạng thái tài khoản đã bị hủy.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Đã xảy ra lỗi khi thay đổi trạng thái tài khoản.";
        }
    }

    private bool CanCreateRole()
    {
        return !IsLoading;
    }

    private bool CanEditRole()
    {
        return !IsLoading
            && SelectedRole is not null;
    }

    private async Task CreateRoleAsync()
    {
        if (IsLoading)
        {
            return;
        }

        ErrorMessage =
            null;

        try
        {
            bool created =
                _dialogService
                    .ShowCreateRoleDialog();

            if (created)
            {
                await LoadAsync();
            }
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể mở màn hình tạo vai trò.";
        }
    }

    private async Task EditRoleAsync()
    {
        AccountManagementRoleItem? selected =
            SelectedRole;

        if (selected is null
            || IsLoading)
        {
            return;
        }

        ErrorMessage =
            null;

        try
        {
            bool updated =
                _dialogService
                    .ShowEditRoleDialog(
                        selected);

            if (updated)
            {
                await LoadAsync();
            }
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể mở màn hình sửa vai trò.";
        }
    }

    private bool CanManageRolePermissions()
    {
        return !IsLoading
            && SelectedRole is not null;
    }

    private async Task ManageRolePermissionsAsync()
    {
        AccountManagementRoleItem? selected =
            SelectedRole;

        if (selected is null
            || IsLoading)
        {
            return;
        }

        ErrorMessage =
            null;

        try
        {
            bool saved =
                _dialogService
                    .ShowManageRolePermissionsDialog(
                        selected.RoleId);

            if (saved)
            {
                await LoadAsync();
            }
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể mở màn hình phân quyền vai trò.";
        }
    }

    private bool CanDeactivateRole()
    {
        return !IsLoading
            && SelectedRole is
            {
                IsActive: true
            };
    }

    private bool CanReactivateRole()
    {
        return !IsLoading
            && SelectedRole is
            {
                IsActive: false
            };
    }

    private Task DeactivateRoleAsync()
    {
        return SetSelectedRoleActiveStateAsync(
            isActive:
                false);
    }

    private Task ReactivateRoleAsync()
    {
        return SetSelectedRoleActiveStateAsync(
            isActive:
                true);
    }

    private async Task SetSelectedRoleActiveStateAsync(
        bool isActive)
    {
        AccountManagementRoleItem? selected =
            SelectedRole;

        if (selected is null
            || IsLoading
            || selected.IsActive ==
                isActive)
        {
            return;
        }

        ErrorMessage =
            null;

        string title =
            isActive
                ? "Kích hoạt lại vai trò"
                : "Ngừng sử dụng vai trò";

        string message =
            isActive
                ? $"Kích hoạt lại vai trò \"{selected.Name}\"?\n\n"
                    + "Các tài khoản đã được gán vai trò này "
                    + "có thể nhận lại các quyền tương ứng."
                : $"Ngừng sử dụng vai trò \"{selected.Name}\"?\n\n"
                    + "Các gán vai trò vẫn được giữ lại, "
                    + "nhưng quyền từ vai trò này sẽ không còn hiệu lực.";

        if (!_confirmationService
                .Confirm(
                    title,
                    message))
        {
            return;
        }

        try
        {
            RoleManagementResult result =
                await _roleActiveStateService
                    .SetAsync(
                        selected.RoleId,
                        isActive);

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể thay đổi trạng thái vai trò.";

                return;
            }

            await LoadAsync();
        }
        catch (AuthorizationDeniedException)
        {
            ErrorMessage =
                "Bạn không có quyền thay đổi trạng thái vai trò.";
        }
        catch (OperationCanceledException)
        {
            ErrorMessage =
                "Thao tác thay đổi trạng thái vai trò đã bị hủy.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Đã xảy ra lỗi khi thay đổi trạng thái vai trò.";
        }
    }

    private static AccountManagementAccountRow?
        PreserveAccountSelection(
            IReadOnlyList<AccountManagementAccountRow> rows,
            AccountManagementAccountRow? previousSelection)
    {
        if (previousSelection is null)
        {
            return null;
        }

        return rows.FirstOrDefault(
            row =>
                row.AccountId ==
                previousSelection.AccountId);
    }

    private static AccountManagementRoleItem?
        PreserveRoleSelection(
            IReadOnlyList<AccountManagementRoleItem> roles,
            AccountManagementRoleItem? previousSelection)
    {
        if (previousSelection is null)
        {
            return null;
        }

        return roles.FirstOrDefault(
            role =>
                role.RoleId ==
                previousSelection.RoleId);
    }
}

public sealed record AccountManagementAccountRow(
    Guid AccountId,
    string Username,
    string DisplayName,
    UserAccountKind Kind,
    string KindText,
    bool IsActive,
    string StatusText,
    Guid? EmployeeId,
    string EmployeeText,
    string RolesText)
{
    public static AccountManagementAccountRow FromItem(
        AccountManagementAccountItem item)
    {
        ArgumentNullException.ThrowIfNull(
            item);

        string employeeText =
            item.EmployeeId.HasValue
                ? string.IsNullOrWhiteSpace(
                    item.EmployeeCode)
                    ? item.EmployeeName
                        ?? "Nhân viên đã liên kết"
                    : string.IsNullOrWhiteSpace(
                        item.EmployeeName)
                        ? item.EmployeeCode
                        : $"{item.EmployeeCode} - {item.EmployeeName}"
                : "Chưa liên kết";

        string rolesText =
            item.Kind ==
                UserAccountKind.Owner
                ? "Không áp dụng (Owner)"
                : item.Roles.Count == 0
                    ? "Chưa gán vai trò"
                    : string.Join(
                        ", ",
                        item.Roles
                            .Select(
                                role =>
                                    role.Name));

        return new AccountManagementAccountRow(
            item.AccountId,
            item.Username,
            item.DisplayName,
            item.Kind,
            item.Kind ==
                UserAccountKind.Owner
                ? "Owner"
                : "Standard",
            item.IsActive,
            item.IsActive
                ? "Đang hoạt động"
                : "Đã vô hiệu hóa",
            item.EmployeeId,
            employeeText,
            rolesText);
    }
}
