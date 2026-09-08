namespace HrManagement.Domain.Authorization.Roles;

public sealed class UserAccountRole
{
    public Guid AccountId { get; }

    public Guid RoleId { get; }

    public UserAccountRole(
        Guid accountId,
        Guid roleId)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã tài khoản không hợp lệ.",
                nameof(accountId));
        }

        if (roleId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã vai trò không hợp lệ.",
                nameof(roleId));
        }

        AccountId =
            accountId;

        RoleId =
            roleId;
    }
}
