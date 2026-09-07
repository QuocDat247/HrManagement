namespace HrManagement.Application.Authentication.Credentials;

public sealed record PasswordPolicyResult(
    bool IsAccepted,
    PasswordPolicyViolation Violation)
{
    public static PasswordPolicyResult Accepted()
    {
        return new PasswordPolicyResult(
            true,
            PasswordPolicyViolation.None);
    }

    public static PasswordPolicyResult Rejected(
        PasswordPolicyViolation violation)
    {
        if (violation ==
            PasswordPolicyViolation.None)
        {
            throw new ArgumentException(
                "Vi phạm mật khẩu không hợp lệ.",
                nameof(violation));
        }

        return new PasswordPolicyResult(
            false,
            violation);
    }
}
