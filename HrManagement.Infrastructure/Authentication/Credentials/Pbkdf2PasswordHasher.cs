using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using HrManagement.Application.Authentication.Credentials;

namespace HrManagement.Infrastructure.Authentication.Credentials;

public sealed class Pbkdf2PasswordHasher
    : IPasswordHasher
{
    private const string FormatVersion =
        "v1";

    private const string AlgorithmName =
        "pbkdf2-sha256";

    private const int CurrentIterationCount =
        600_000;

    private const int MaximumAcceptedIterationCount =
        5_000_000;

    private const int SaltSize =
        16;

    private const int DerivedKeySize =
        32;

    public string HashPassword(
        string password)
    {
        ArgumentNullException.ThrowIfNull(
            password);

        string normalizedPassword =
            password.Normalize(
                NormalizationForm.FormC);

        byte[] salt =
            RandomNumberGenerator.GetBytes(
                SaltSize);

        byte[] derivedKey =
            Rfc2898DeriveBytes.Pbkdf2(
                normalizedPassword,
                salt,
                CurrentIterationCount,
                HashAlgorithmName.SHA256,
                DerivedKeySize);

        try
        {
            return string.Join(
                '$',
                FormatVersion,
                AlgorithmName,
                CurrentIterationCount.ToString(
                    CultureInfo.InvariantCulture),
                Convert.ToBase64String(
                    salt),
                Convert.ToBase64String(
                    derivedKey));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                derivedKey);
        }
    }

    public PasswordVerificationResult VerifyPassword(
        string password,
        string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(
            password);

        string normalizedPassword =
            password.Normalize(
                NormalizationForm.FormC);

        if (string.IsNullOrWhiteSpace(
                passwordHash))
        {
            return PasswordVerificationResult.Failed;
        }

        string[] parts =
            passwordHash.Split(
                '$');

        if (parts.Length != 5
            || !string.Equals(
                parts[0],
                FormatVersion,
                StringComparison.Ordinal)
            || !string.Equals(
                parts[1],
                AlgorithmName,
                StringComparison.Ordinal))
        {
            return PasswordVerificationResult.Failed;
        }

        if (!int.TryParse(
                parts[2],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int iterationCount)
            || iterationCount <= 0
            || iterationCount >
                MaximumAcceptedIterationCount)
        {
            return PasswordVerificationResult.Failed;
        }

        byte[] salt;
        byte[] expectedDerivedKey;

        try
        {
            salt =
                Convert.FromBase64String(
                    parts[3]);

            expectedDerivedKey =
                Convert.FromBase64String(
                    parts[4]);
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }

        if (salt.Length != SaltSize
            || expectedDerivedKey.Length !=
                DerivedKeySize)
        {
            return PasswordVerificationResult.Failed;
        }

        byte[] actualDerivedKey =
            Rfc2898DeriveBytes.Pbkdf2(
                normalizedPassword,
                salt,
                iterationCount,
                HashAlgorithmName.SHA256,
                DerivedKeySize);

        try
        {
            bool isValid =
                CryptographicOperations.FixedTimeEquals(
                    actualDerivedKey,
                    expectedDerivedKey);

            if (!isValid)
            {
                return PasswordVerificationResult.Failed;
            }

            return iterationCount <
                CurrentIterationCount
                ? PasswordVerificationResult
                    .SuccessRehashNeeded
                : PasswordVerificationResult
                    .Success;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                actualDerivedKey);
        }
    }
}
