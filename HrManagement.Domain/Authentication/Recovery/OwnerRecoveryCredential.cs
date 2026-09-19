namespace HrManagement.Domain.Authentication.Recovery;

public sealed class OwnerRecoveryCredential
{
    public Guid AccountId
    {
        get;
    }

    public string RecoveryCodeHash
    {
        get;
    }

    public OwnerRecoveryCredential(
        Guid accountId,
        string recoveryCodeHash)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã tài khoản không hợp lệ.",
                nameof(accountId));
        }

        if (string.IsNullOrWhiteSpace(
                recoveryCodeHash))
        {
            throw new ArgumentException(
                "Recovery code hash không được để trống.",
                nameof(recoveryCodeHash));
        }

        if (recoveryCodeHash.Length > 1024)
        {
            throw new ArgumentException(
                "Recovery code hash vượt quá giới hạn cho phép.",
                nameof(recoveryCodeHash));
        }

        if (recoveryCodeHash.Any(
                char.IsWhiteSpace))
        {
            throw new ArgumentException(
                "Recovery code hash không được chứa khoảng trắng.",
                nameof(recoveryCodeHash));
        }

        AccountId =
            accountId;

        RecoveryCodeHash =
            recoveryCodeHash;
    }
}
