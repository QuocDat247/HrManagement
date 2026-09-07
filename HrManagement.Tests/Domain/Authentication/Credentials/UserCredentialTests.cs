using HrManagement.Domain.Authentication.Credentials;

namespace HrManagement.Tests.Domain.Authentication.Credentials;

public sealed class UserCredentialTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesCredential()
    {
        Guid accountId =
            Guid.NewGuid();

        const string passwordHash =
            "$example$opaque$hash";

        var credential =
            new UserCredential(
                accountId,
                passwordHash);

        Assert.Equal(
            accountId,
            credential.AccountId);

        Assert.Equal(
            passwordHash,
            credential.PasswordHash);

        Assert.True(
            credential.MustChangePassword);
    }

    [Fact]
    public void Constructor_WithPermanentPassword_PreservesState()
    {
        var credential =
            new UserCredential(
                Guid.NewGuid(),
                "$example$opaque$hash",
                mustChangePassword: false);

        Assert.False(
            credential.MustChangePassword);
    }

    [Fact]
    public void Constructor_WithEmptyAccountId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new UserCredential(
                Guid.Empty,
                "$example$opaque$hash"));
    }

    [Fact]
    public void Constructor_WithBlankPasswordHash_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new UserCredential(
                Guid.NewGuid(),
                "   "));
    }

    [Fact]
    public void Constructor_WithWhitespaceInPasswordHash_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new UserCredential(
                Guid.NewGuid(),
                "$example$ opaque$hash"));
    }

    [Fact]
    public void Constructor_WithOversizedPasswordHash_Throws()
    {
        string passwordHash =
            new(
                'a',
                1025);

        Assert.Throws<ArgumentException>(
            () => new UserCredential(
                Guid.NewGuid(),
                passwordHash));
    }
}
