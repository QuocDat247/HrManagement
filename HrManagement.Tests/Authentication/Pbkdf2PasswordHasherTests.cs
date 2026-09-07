using System.Globalization;
using System.Security.Cryptography;
using HrManagement.Application.Authentication.Credentials;
using HrManagement.Infrastructure.Authentication.Credentials;

namespace HrManagement.Tests.Authentication;

public sealed class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher
        _hasher =
            new();

    [Fact]
    public void
        HashPassword_SamePasswordTwice_CreatesDifferentHashes()
    {
        const string password =
            "ExamplePassword123!";

        string firstHash =
            _hasher.HashPassword(
                password);

        string secondHash =
            _hasher.HashPassword(
                password);

        Assert.NotEqual(
            firstHash,
            secondHash);
    }

    [Fact]
    public void
        VerifyPassword_WithCorrectPassword_ReturnsSuccess()
    {
        const string password =
            "ExamplePassword123!";

        string passwordHash =
            _hasher.HashPassword(
                password);

        PasswordVerificationResult result =
            _hasher.VerifyPassword(
                password,
                passwordHash);

        Assert.Equal(
            PasswordVerificationResult.Success,
            result);
    }

    [Fact]
    public void
        VerifyPassword_WithIncorrectPassword_ReturnsFailed()
    {
        string passwordHash =
            _hasher.HashPassword(
                "CorrectPassword123!");

        PasswordVerificationResult result =
            _hasher.VerifyPassword(
                "WrongPassword123!",
                passwordHash);

        Assert.Equal(
            PasswordVerificationResult.Failed,
            result);
    }

    [Fact]
    public void
        VerifyPassword_EquivalentUnicodeForms_ReturnsSuccess()
    {
        const string composedPassword =
            "Café password 123";

        const string decomposedPassword =
            "Cafe\u0301 password 123";

        string passwordHash =
            _hasher.HashPassword(
                composedPassword);

        PasswordVerificationResult result =
            _hasher.VerifyPassword(
                decomposedPassword,
                passwordHash);

        Assert.Equal(
            PasswordVerificationResult.Success,
            result);
    }

    [Fact]
    public void
        VerifyPassword_PreservesPasswordWhitespace()
    {
        const string password =
            " ExamplePassword123! ";

        string passwordHash =
            _hasher.HashPassword(
                password);

        Assert.Equal(
            PasswordVerificationResult.Success,
            _hasher.VerifyPassword(
                password,
                passwordHash));

        Assert.Equal(
            PasswordVerificationResult.Failed,
            _hasher.VerifyPassword(
                password.Trim(),
                passwordHash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("v1$pbkdf2-sha256")]
    [InlineData("v1$pbkdf2-sha256$abc$bad$bad")]
    public void
        VerifyPassword_WithMalformedHash_ReturnsFailed(
            string passwordHash)
    {
        PasswordVerificationResult result =
            _hasher.VerifyPassword(
                "ExamplePassword123!",
                passwordHash);

        Assert.Equal(
            PasswordVerificationResult.Failed,
            result);
    }

    [Fact]
    public void
        VerifyPassword_WithOlderWorkFactor_ReturnsRehashNeeded()
    {
        const string password =
            "ExamplePassword123!";

        string legacyHash =
            CreateHash(
                password,
                iterationCount: 100_000);

        PasswordVerificationResult result =
            _hasher.VerifyPassword(
                password,
                legacyHash);

        Assert.Equal(
            PasswordVerificationResult
                .SuccessRehashNeeded,
            result);
    }

    private static string CreateHash(
        string password,
        int iterationCount)
    {
        byte[] salt =
            RandomNumberGenerator.GetBytes(
                16);

        byte[] derivedKey =
            Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterationCount,
                HashAlgorithmName.SHA256,
                32);

        try
        {
            return string.Join(
                '$',
                "v1",
                "pbkdf2-sha256",
                iterationCount.ToString(
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
}
