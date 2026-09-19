using HrManagement.Application.Authentication.Credentials;

namespace HrManagement.Application.Authentication.Recovery;

public sealed class RecoveryCodeHasher
    : IRecoveryCodeHasher
{
    private readonly IPasswordHasher
        _passwordHasher;

    public RecoveryCodeHasher(
        IPasswordHasher passwordHasher)
    {
        _passwordHasher =
            passwordHasher;
    }

    public string Hash(
        string recoveryCode)
    {
        string? normalized =
            RecoveryCodeFormat.Normalize(
                recoveryCode);

        if (normalized is null)
        {
            throw new ArgumentException(
                "Recovery code không hợp lệ.",
                nameof(recoveryCode));
        }

        return _passwordHasher.HashPassword(
            normalized);
    }

    public bool Verify(
        string recoveryCode,
        string recoveryCodeHash)
    {
        string? normalized =
            RecoveryCodeFormat.Normalize(
                recoveryCode);

        if (normalized is null
            || string.IsNullOrWhiteSpace(
                recoveryCodeHash))
        {
            return false;
        }

        PasswordVerificationResult result =
            _passwordHasher.VerifyPassword(
                normalized,
                recoveryCodeHash);

        return result !=
            PasswordVerificationResult.Failed;
    }
}
