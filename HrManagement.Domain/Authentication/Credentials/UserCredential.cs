namespace HrManagement.Domain.Authentication.Credentials;

public sealed class UserCredential
{
    public Guid AccountId { get; }

    public string PasswordHash { get; }

    public bool MustChangePassword { get; }

    public UserCredential(
        Guid accountId,
        string passwordHash,
        bool mustChangePassword = true)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã tài khoản không hợp lệ.",
                nameof(accountId));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException(
                "Password hash không được để trống.",
                nameof(passwordHash));
        }

        if (passwordHash.Length > 1024)
        {
            throw new ArgumentException(
                "Password hash vượt quá giới hạn cho phép.",
                nameof(passwordHash));
        }

        if (passwordHash.Any(
                char.IsWhiteSpace))
        {
            throw new ArgumentException(
                "Password hash không được chứa khoảng trắng.",
                nameof(passwordHash));
        }

        AccountId =
            accountId;

        PasswordHash =
            passwordHash;

        MustChangePassword =
            mustChangePassword;
    }
}
