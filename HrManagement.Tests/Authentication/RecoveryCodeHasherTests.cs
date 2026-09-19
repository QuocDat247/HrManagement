using HrManagement.Application.Authentication.Credentials;
using HrManagement.Application.Authentication.Recovery;

namespace HrManagement.Tests.Authentication;

public sealed class RecoveryCodeHasherTests
{
    [Fact]
    public void
        Verify_WithEquivalentFormatting_ReturnsTrue()
    {
        var passwordHasher =
            new TestPasswordHasher();

        var hasher =
            new RecoveryCodeHasher(
                passwordHasher);

        string hash =
            hasher.Hash(
                "7F3A-91C8-2D6E-B447-A120-8F9C-35D2-61EA");

        bool verified =
            hasher.Verify(
                "7f3a 91c8 2d6e b447 a120 8f9c 35d2 61ea",
                hash);

        Assert.True(
            verified);
    }

    [Fact]
    public void
        Verify_WithInvalidCode_ReturnsFalse()
    {
        var hasher =
            new RecoveryCodeHasher(
                new TestPasswordHasher());

        Assert.False(
            hasher.Verify(
                "invalid",
                "$test$hash"));
    }

    private sealed class TestPasswordHasher
        : IPasswordHasher
    {
        public string HashPassword(
            string password)
        {
            return "$test$"
                + password;
        }

        public PasswordVerificationResult
            VerifyPassword(
                string password,
                string passwordHash)
        {
            return string.Equals(
                    passwordHash,
                    "$test$" + password,
                    StringComparison.Ordinal)
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }
    }
}
