namespace HrManagement.Domain.Authentication.Accounts;

public sealed class UserAccount
{
    public Guid Id { get; }

    public string Username { get; }

    public string NormalizedUsername { get; }

    public string DisplayName
    {
        get;
        private set;
    }

    public Guid? EmployeeId
    {
        get;
        private set;
    }

    public UserAccountKind Kind { get; }

    public bool IsActive
    {
        get;
        private set;
    }

    public UserAccount(
        Guid id,
        string username,
        string displayName,
        UserAccountKind kind,
        Guid? employeeId = null,
        bool isActive = true)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã định danh tài khoản không hợp lệ.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException(
                "Tên đăng nhập không được để trống.",
                nameof(username));
        }

        string trimmedUsername =
            username.Trim();

        if (trimmedUsername.Any(
                char.IsWhiteSpace))
        {
            throw new ArgumentException(
                "Tên đăng nhập không được chứa khoảng trắng.",
                nameof(username));
        }

        if (trimmedUsername.Length > 100)
        {
            throw new ArgumentException(
                "Tên đăng nhập không được vượt quá 100 ký tự.",
                nameof(username));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException(
                "Tên hiển thị không được để trống.",
                nameof(displayName));
        }

        string trimmedDisplayName =
            displayName.Trim();

        if (trimmedDisplayName.Length > 200)
        {
            throw new ArgumentException(
                "Tên hiển thị không được vượt quá 200 ký tự.",
                nameof(displayName));
        }

        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên liên kết không hợp lệ.",
                nameof(employeeId));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind));
        }

        Id =
            id;

        Username =
            trimmedUsername;

        NormalizedUsername =
            trimmedUsername.ToUpperInvariant();

        DisplayName =
            trimmedDisplayName;

        EmployeeId =
            employeeId;

        Kind =
            kind;

        IsActive =
            isActive;
    }

    public void UpdateProfile(
        string displayName,
        Guid? employeeId = null)
    {
        if (string.IsNullOrWhiteSpace(
                displayName))
        {
            throw new ArgumentException(
                "Tên hiển thị không được để trống.",
                nameof(displayName));
        }

        string trimmedDisplayName =
            displayName.Trim();

        if (trimmedDisplayName.Length > 200)
        {
            throw new ArgumentException(
                "Tên hiển thị không được vượt quá 200 ký tự.",
                nameof(displayName));
        }

        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên liên kết không hợp lệ.",
                nameof(employeeId));
        }

        DisplayName =
            trimmedDisplayName;

        EmployeeId =
            employeeId;
    }
    public void SetActive(
        bool isActive)
    {
        IsActive =
            isActive;
    }
}
