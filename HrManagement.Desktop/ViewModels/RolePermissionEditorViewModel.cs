using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Authorization;
using HrManagement.Application.Authorization.Roles;

namespace HrManagement.Desktop.ViewModels;

public sealed class RolePermissionSelectionItem
    : ObservableObject
{
    private bool _isSelected;

    public RolePermissionSelectionItem(
        string code,
        string category,
        bool isSelected)
    {
        Code =
            code;

        Category =
            category;

        _isSelected =
            isSelected;
    }

    public string Code
    {
        get;
    }

    public string Category
    {
        get;
    }

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

public sealed class RolePermissionEditorViewModel
    : ObservableObject
{
    private readonly
        IRolePermissionManagementQueryService
        _queryService;

    private readonly
        IRolePermissionAssignmentService
        _assignmentService;

    private Guid _roleId;

    private string _roleName =
        string.Empty;

    private string _roleStatusText =
        string.Empty;

    private IReadOnlyList<RolePermissionSelectionItem>
        _permissions =
            Array.Empty<RolePermissionSelectionItem>();

    private string? _errorMessage;

    private bool _isBusy;

    private bool _isReady;

    public RolePermissionEditorViewModel(
        IRolePermissionManagementQueryService queryService,
        IRolePermissionAssignmentService assignmentService)
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
        PermissionsSaved;

    public string RoleName
    {
        get =>
            _roleName;

        private set =>
            SetProperty(
                ref _roleName,
                value);
    }

    public string RoleStatusText
    {
        get =>
            _roleStatusText;

        private set =>
            SetProperty(
                ref _roleStatusText,
                value);
    }

    public IReadOnlyList<RolePermissionSelectionItem>
        Permissions
    {
        get =>
            _permissions;

        private set =>
            SetProperty(
                ref _permissions,
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
        Guid roleId)
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage =
            null;

        IsReady =
            false;

        Permissions =
            Array.Empty<RolePermissionSelectionItem>();

        RoleName =
            string.Empty;

        RoleStatusText =
            string.Empty;

        if (roleId == Guid.Empty)
        {
            ErrorMessage =
                "Vai trò không hợp lệ.";

            return;
        }

        IsBusy =
            true;

        try
        {
            RolePermissionManagementSnapshot? snapshot =
                await _queryService
                    .GetAsync(
                        roleId);

            if (snapshot is null)
            {
                ErrorMessage =
                    "Không tìm thấy vai trò.";

                return;
            }

            _roleId =
                snapshot.RoleId;

            RoleName =
                snapshot.RoleName;

            RoleStatusText =
                snapshot.IsActive
                    ? "Đang sử dụng"
                    : "Ngừng sử dụng";

            HashSet<string> assigned =
                snapshot.AssignedPermissionCodes
                    .ToHashSet(
                        StringComparer.Ordinal);

            Permissions =
                snapshot.AvailablePermissionCodes
                    .Select(
                        code =>
                            new RolePermissionSelectionItem(
                                code,
                                GetCategoryText(
                                    code),
                                assigned.Contains(
                                    code)))
                    .ToArray();

            IsReady =
                true;
        }
        catch (AuthorizationDeniedException)
        {
            ErrorMessage =
                "Bạn không có quyền quản lý quyền của vai trò.";
        }
        catch (OperationCanceledException)
        {
            ErrorMessage =
                "Thao tác tải quyền đã bị hủy.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Không thể tải danh sách quyền của vai trò.";
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
            || _roleId == Guid.Empty)
        {
            return;
        }

        ErrorMessage =
            null;

        try
        {
            IsBusy =
                true;

            string[] selectedPermissionCodes =
                Permissions
                    .Where(
                        permission =>
                            permission.IsSelected)
                    .Select(
                        permission =>
                            permission.Code)
                    .OrderBy(
                        code =>
                            code,
                        StringComparer.Ordinal)
                    .ToArray();

            RoleManagementResult result =
                await _assignmentService
                    .ReplaceAsync(
                        _roleId,
                        selectedPermissionCodes);

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể cập nhật quyền của vai trò.";

                return;
            }

            PermissionsSaved?.Invoke(
                this,
                EventArgs.Empty);
        }
        catch (AuthorizationDeniedException)
        {
            ErrorMessage =
                "Bạn không có quyền thay đổi quyền của vai trò.";
        }
        catch (OperationCanceledException)
        {
            ErrorMessage =
                "Thao tác cập nhật quyền đã bị hủy.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Đã xảy ra lỗi khi cập nhật quyền của vai trò.";
        }
        finally
        {
            IsBusy =
                false;
        }
    }

    private static string GetCategoryText(
        string permissionCode)
    {
        int separatorIndex =
            permissionCode.IndexOf(
                '.');

        string prefix =
            separatorIndex > 0
                ? permissionCode[
                    ..separatorIndex]
                : permissionCode;

        return prefix switch
        {
            "Employee" =>
                "Nhân viên",

            "Department" =>
                "Phòng ban",

            "Position" =>
                "Chức danh",

            "WorkSchedule" =>
                "Lịch làm việc",

            "HolidayException" =>
                "Ngày nghỉ / Ngoại lệ",

            "Timesheet" =>
                "Bảng công",

            "Overtime" =>
                "Tăng ca",

            "Attendance" =>
                "Chấm công",

            "Leave" =>
                "Nghỉ phép",

            "Payroll" =>
                "Tiền lương",

            "Report" =>
                "Báo cáo",

            "Role" =>
                "Vai trò",

            "Account" =>
                "Tài khoản",

            "Settings" =>
                "Cài đặt",

            _ =>
                prefix
        };
    }
}
