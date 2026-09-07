using HrManagement.Infrastructure.Authentication.Credentials;

namespace HrManagement.Tests.Authentication;

public sealed class BundledPasswordBlocklistTests
{
    private readonly BundledPasswordBlocklist
        _blocklist =
            new();

    [Theory]
    [InlineData("passwordpassword")]
    [InlineData("PASSWORDPASSWORD")]
    [InlineData("changeme123456789")]
    [InlineData("defaultpassword123")]
    public void
        IsBlocked_WithBundledPassword_ReturnsTrue(
            string password)
    {
        Assert.True(
            _blocklist.IsBlocked(
                password));
    }

    [Fact]
    public void
        IsBlocked_WithUnknownPassword_ReturnsFalse()
    {
        Assert.False(
            _blocklist.IsBlocked(
                "A unique passphrase for payroll 2026"));
    }

    [Fact]
    public void
        IsBlocked_DoesNotRejectPartialMatch()
    {
        Assert.False(
            _blocklist.IsBlocked(
                "My passwordpassword is much longer"));
    }

    [Fact]
    public void
        PasswordPolicy_WithBundledPassword_ReturnsBlocked()
    {
        var policy =
            new HrManagement.Application.Authentication.Credentials
                .DefaultPasswordPolicy(
                    _blocklist);

        var result =
            policy.Evaluate(
                "passwordpassword");

        Assert.False(
            result.IsAccepted);

        Assert.Equal(
            HrManagement.Application.Authentication.Credentials
                .PasswordPolicyViolation.Blocked,
            result.Violation);
    }
}
