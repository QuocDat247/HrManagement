using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Desktop.Services.Accounts;
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

    public AccountManagementWorkspaceViewModel(
        IAccountManagementQueryService queryService,
        IAccountManagementDialogService dialogService)
    {
        _queryService =
            queryService;

        _dialogService =
            dialogService;

        RefreshCommand =
            new AsyncRelayCommand(
                LoadAsync);

        RefreshCommand =
            new AsyncRelayCommand(
                LoadAsync);

        CreateAccountCommand =
            new AsyncRelayCommand(
                CreateAccountAsync);
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
