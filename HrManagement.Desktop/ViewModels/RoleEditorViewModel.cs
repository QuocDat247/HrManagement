using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authorization;
using HrManagement.Application.Authorization.Roles;

namespace HrManagement.Desktop.ViewModels;

public sealed class RoleEditorViewModel
    : ObservableObject
{
    private readonly IRoleManagementService
        _roleManagementService;

    private Guid? _roleId;

    private bool _isEditMode;

    private string _name =
        string.Empty;

    private string _description =
        string.Empty;

    private string? _errorMessage;

    private bool _isBusy;

    private bool _isReady;

    public RoleEditorViewModel(
        IRoleManagementService roleManagementService)
    {
        _roleManagementService =
            roleManagementService;

        SaveCommand =
            new AsyncRelayCommand(
                SaveAsync,
                CanSave);
    }

    public event EventHandler?
        RoleSaved;

    public bool IsEditMode
    {
        get =>
            _isEditMode;

        private set
        {
            if (SetProperty(
                    ref _isEditMode,
                    value))
            {
                OnPropertyChanged(
                    nameof(WindowTitle));

                OnPropertyChanged(
                    nameof(HeadingText));

                OnPropertyChanged(
                    nameof(SubmitText));
            }
        }
    }

    public string Name
    {
        get =>
            _name;

        set
        {
            if (SetProperty(
                    ref _name,
                    value))
            {
                ErrorMessage =
                    null;

                OnPropertyChanged(
                    nameof(CanSubmit));

                SaveCommand
                    .NotifyCanExecuteChanged();
            }
        }
    }

    public string Description
    {
        get =>
            _description;

        set
        {
            if (SetProperty(
                    ref _description,
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

    public string WindowTitle =>
        IsEditMode
            ? "Sửa vai trò - HR Management"
            : "Tạo vai trò - HR Management";

    public string HeadingText =>
        IsEditMode
            ? "Sửa vai trò"
            : "Tạo vai trò";

    public string SubmitText =>
        IsEditMode
            ? "Lưu thay đổi"
            : "Tạo vai trò";

    public bool CanSubmit =>
        IsReady
        && !IsBusy
        && !string.IsNullOrWhiteSpace(
            Name);

    public IAsyncRelayCommand SaveCommand
    {
        get;
    }

    public void LoadForCreate()
    {
        _roleId =
            null;

        IsReady =
            false;

        IsEditMode =
            false;

        Name =
            string.Empty;

        Description =
            string.Empty;

        ErrorMessage =
            null;

        IsReady =
            true;
    }

    public void LoadForEdit(
        AccountManagementRoleItem role)
    {
        ArgumentNullException.ThrowIfNull(
            role);

        if (role.RoleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã vai trò không hợp lệ.",
                nameof(role));
        }

        IsReady =
            false;

        _roleId =
            role.RoleId;

        IsEditMode =
            true;

        Name =
            role.Name;

        Description =
            role.Description
            ?? string.Empty;

        ErrorMessage =
            null;

        IsReady =
            true;
    }

    private bool CanSave()
    {
        return CanSubmit;
    }

    private async Task SaveAsync()
    {
        if (!CanSubmit)
        {
            return;
        }

        ErrorMessage =
            null;

        try
        {
            IsBusy =
                true;

            RoleManagementResult result;

            if (IsEditMode)
            {
                if (!_roleId.HasValue
                    || _roleId.Value ==
                        Guid.Empty)
                {
                    ErrorMessage =
                        "Vai trò không hợp lệ.";

                    return;
                }

                result =
                    await _roleManagementService
                        .UpdateAsync(
                            _roleId.Value,
                            Name,
                            Description);
            }
            else
            {
                result =
                    await _roleManagementService
                        .CreateAsync(
                            Name,
                            Description);
            }

            if (!result.IsSuccessful)
            {
                ErrorMessage =
                    result.ErrorMessage
                    ?? "Không thể lưu vai trò.";

                return;
            }

            RoleSaved?.Invoke(
                this,
                EventArgs.Empty);
        }
        catch (AuthorizationDeniedException)
        {
            ErrorMessage =
                IsEditMode
                    ? "Bạn không có quyền sửa vai trò."
                    : "Bạn không có quyền tạo vai trò.";
        }
        catch (OperationCanceledException)
        {
            ErrorMessage =
                "Thao tác đã bị hủy.";
        }
        catch (Exception)
        {
            ErrorMessage =
                "Đã xảy ra lỗi khi lưu vai trò. Vui lòng thử lại.";
        }
        finally
        {
            IsBusy =
                false;
        }
    }
}
