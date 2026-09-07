namespace HrManagement.Domain.Authentication.Security;

public sealed class UserLoginSecurityState
{
    public Guid AccountId { get; }

    public int FailedLoginCount { get; }

    public DateTimeOffset? LockoutEndUtc { get; }

    public UserLoginSecurityState(
        Guid accountId,
        int failedLoginCount = 0,
        DateTimeOffset? lockoutEndUtc = null)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã tài khoản không hợp lệ.",
                nameof(accountId));
        }

        if (failedLoginCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failedLoginCount));
        }

        AccountId =
            accountId;

        FailedLoginCount =
            failedLoginCount;

        LockoutEndUtc =
            lockoutEndUtc?.ToUniversalTime();
    }

    public bool IsLockedOut(
        DateTimeOffset nowUtc)
    {
        return LockoutEndUtc
            is DateTimeOffset lockoutEndUtc
            && nowUtc.ToUniversalTime() <
                lockoutEndUtc;
    }
}
