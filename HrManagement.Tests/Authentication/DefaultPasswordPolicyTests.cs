using HrManagement.Application.Authentication.Credentials;

namespace HrManagement.Tests.Authentication;

public sealed class DefaultPasswordPolicyTests
{
    [Fact]
    public void
        Evaluate_WithMinimumLength_ReturnsAccepted()
    {
        var policy =
            CreatePolicy();

        PasswordPolicyResult result =
            policy.Evaluate(
                "123456789012345");

        Assert.True(
            result.IsAccepted);

        Assert.Equal(
            PasswordPolicyViolation.None,
            result.Violation);
    }

    [Fact]
    public void
        Evaluate_WithTooShortPassword_ReturnsTooShort()
    {
        var policy =
            CreatePolicy();

        PasswordPolicyResult result =
            policy.Evaluate(
                "12345678901234");

        Assert.False(
            result.IsAccepted);

        Assert.Equal(
            PasswordPolicyViolation.TooShort,
            result.Violation);
    }

    [Fact]
    public void
        Evaluate_WithMaximumLength_ReturnsAccepted()
    {
        var policy =
            CreatePolicy();

        string password =
            new(
                'a',
                DefaultPasswordPolicy.MaximumLength);

        PasswordPolicyResult result =
            policy.Evaluate(
                password);

        Assert.True(
            result.IsAccepted);
    }

    [Fact]
    public void
        Evaluate_WithTooLongPassword_ReturnsTooLong()
    {
        var policy =
            CreatePolicy();

        string password =
            new(
                'a',
                DefaultPasswordPolicy.MaximumLength + 1);

        PasswordPolicyResult result =
            policy.Evaluate(
                password);

        Assert.False(
            result.IsAccepted);

        Assert.Equal(
            PasswordPolicyViolation.TooLong,
            result.Violation);
    }

    [Fact]
    public void
        Evaluate_WithUnicodeAndSpaces_ReturnsAccepted()
    {
        var policy =
            CreatePolicy();

        PasswordPolicyResult result =
            policy.Evaluate(
                "Mật khẩu của tôi rất dài");

        Assert.True(
            result.IsAccepted);
    }

    [Fact]
    public void
        Evaluate_CountsUnicodeCodePoints()
    {
        var policy =
            CreatePolicy();

        string password =
            string.Concat(
                Enumerable.Repeat(
                    "😀",
                    DefaultPasswordPolicy.MinimumLength));

        PasswordPolicyResult result =
            policy.Evaluate(
                password);

        Assert.True(
            result.IsAccepted);
    }

    [Fact]
    public void
        Evaluate_WithBlockedPassword_ReturnsBlocked()
    {
        var policy =
            CreatePolicy(
                "blocked password value");

        PasswordPolicyResult result =
            policy.Evaluate(
                "blocked password value");

        Assert.False(
            result.IsAccepted);

        Assert.Equal(
            PasswordPolicyViolation.Blocked,
            result.Violation);
    }

    [Fact]
    public void
        Evaluate_WhenPasswordEqualsUsername_ReturnsBlocked()
    {
        var policy =
            CreatePolicy();

        PasswordPolicyResult result =
            policy.Evaluate(
                "commercial-owner",
                username: "Commercial-Owner");

        Assert.False(
            result.IsAccepted);

        Assert.Equal(
            PasswordPolicyViolation.Blocked,
            result.Violation);
    }

    [Fact]
    public void
        Evaluate_WhenPasswordOnlyContainsUsername_DoesNotRejectIt()
    {
        var policy =
            CreatePolicy();

        PasswordPolicyResult result =
            policy.Evaluate(
                "A long passphrase for owner",
                username: "owner");

        Assert.True(
            result.IsAccepted);
    }

    private static DefaultPasswordPolicy CreatePolicy(
        params string[] blockedPasswords)
    {
        return new DefaultPasswordPolicy(
            new TestPasswordBlocklist(
                blockedPasswords));
    }

    private sealed class TestPasswordBlocklist
        : IPasswordBlocklist
    {
        private readonly HashSet<string>
            _blockedPasswords;

        public TestPasswordBlocklist(
            IEnumerable<string> blockedPasswords)
        {
            _blockedPasswords =
                new HashSet<string>(
                    blockedPasswords,
                    StringComparer.OrdinalIgnoreCase);
        }

        public bool IsBlocked(
            string normalizedPassword)
        {
            return _blockedPasswords.Contains(
                normalizedPassword);
        }
    }
}
