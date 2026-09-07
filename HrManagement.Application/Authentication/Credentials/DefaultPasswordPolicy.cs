using System.Text;

namespace HrManagement.Application.Authentication.Credentials;

public sealed class DefaultPasswordPolicy
    : IPasswordPolicy
{
    public const int MinimumLength =
        15;

    public const int MaximumLength =
        128;

    private readonly IPasswordBlocklist
        _passwordBlocklist;

    public DefaultPasswordPolicy(
        IPasswordBlocklist passwordBlocklist)
    {
        _passwordBlocklist =
            passwordBlocklist;
    }

    public PasswordPolicyResult Evaluate(
        string password,
        string? username = null)
    {
        ArgumentNullException.ThrowIfNull(
            password);

        string normalizedPassword =
            password.Normalize(
                NormalizationForm.FormC);

        int passwordLength =
            normalizedPassword
                .EnumerateRunes()
                .Count();

        if (passwordLength <
            MinimumLength)
        {
            return PasswordPolicyResult.Rejected(
                PasswordPolicyViolation.TooShort);
        }

        if (passwordLength >
            MaximumLength)
        {
            return PasswordPolicyResult.Rejected(
                PasswordPolicyViolation.TooLong);
        }

        if (_passwordBlocklist.IsBlocked(
                normalizedPassword))
        {
            return PasswordPolicyResult.Rejected(
                PasswordPolicyViolation.Blocked);
        }

        if (!string.IsNullOrWhiteSpace(
                username))
        {
            string normalizedUsername =
                username.Trim()
                    .Normalize(
                        NormalizationForm.FormC);

            if (string.Equals(
                    normalizedPassword,
                    normalizedUsername,
                    StringComparison.OrdinalIgnoreCase))
            {
                return PasswordPolicyResult.Rejected(
                    PasswordPolicyViolation.Blocked);
            }
        }

        return PasswordPolicyResult.Accepted();
    }
}
