namespace HrManagement.Domain.Authorization.Permissions;

public sealed record Permission
{
    public string Code { get; }

    public Permission(
        string code)
    {
        if (string.IsNullOrWhiteSpace(
                code))
        {
            throw new ArgumentException(
                "Mã quyền không được để trống.",
                nameof(code));
        }

        string trimmedCode =
            code.Trim();

        if (!PermissionCodes.All.Contains(
                trimmedCode))
        {
            throw new ArgumentException(
                "Mã quyền không được hỗ trợ.",
                nameof(code));
        }

        Code =
            trimmedCode;
    }

    public override string ToString()
    {
        return Code;
    }
}
