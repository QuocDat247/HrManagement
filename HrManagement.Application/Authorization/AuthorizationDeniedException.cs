namespace HrManagement.Application.Authorization;

public sealed class AuthorizationDeniedException
    : Exception
{
    public AuthorizationDeniedException(
        string permissionCode)
        : base(
            "Bạn không có quyền thực hiện thao tác này.")
    {
        if (string.IsNullOrWhiteSpace(
                permissionCode))
        {
            throw new ArgumentException(
                "Mã quyền không được để trống.",
                nameof(permissionCode));
        }

        PermissionCode =
            permissionCode;
    }

    public string PermissionCode
    {
        get;
    }
}
