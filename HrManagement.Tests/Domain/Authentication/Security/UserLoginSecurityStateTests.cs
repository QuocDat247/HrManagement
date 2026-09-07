using HrManagement.Domain.Authentication.Security;

namespace HrManagement.Tests.Domain.Authentication.Security;

public sealed class UserLoginSecurityStateTests
{
    [Fact]
    public void
        Constructor_WithDefaultValues_CreatesUnlockedState()
    {
        Guid accountId =
            Guid.NewGuid();

        var state =
            new UserLoginSecurityState(
                accountId);

        Assert.Equal(
            accountId,
            state.AccountId);

        Assert.Equal(
            0,
            state.FailedLoginCount);

        Assert.Null(
            state.LockoutEndUtc);
    }

    [Fact]
    public void
        Constructor_WithEmptyAccountId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new UserLoginSecurityState(
                    Guid.Empty));
    }

    [Fact]
    public void
        Constructor_WithNegativeFailureCount_Throws()
    {
        Assert.Throws<
            ArgumentOutOfRangeException>(
            () =>
                new UserLoginSecurityState(
                    Guid.NewGuid(),
                    failedLoginCount: -1));
    }

    [Fact]
    public void
        Constructor_NormalizesLockoutEndToUtc()
    {
        DateTimeOffset lockoutEnd =
            new(
                2026,
                9,
                7,
                18,
                0,
                0,
                TimeSpan.FromHours(7));

        var state =
            new UserLoginSecurityState(
                Guid.NewGuid(),
                failedLoginCount: 5,
                lockoutEndUtc: lockoutEnd);

        Assert.Equal(
            TimeSpan.Zero,
            state.LockoutEndUtc!.Value.Offset);

        Assert.Equal(
            lockoutEnd.ToUniversalTime(),
            state.LockoutEndUtc);
    }

    [Fact]
    public void
        IsLockedOut_BeforeLockoutEnd_ReturnsTrue()
    {
        DateTimeOffset now =
            new(
                2026,
                9,
                7,
                10,
                0,
                0,
                TimeSpan.Zero);

        var state =
            new UserLoginSecurityState(
                Guid.NewGuid(),
                failedLoginCount: 5,
                lockoutEndUtc:
                    now.AddMinutes(15));

        Assert.True(
            state.IsLockedOut(
                now));
    }

    [Fact]
    public void
        IsLockedOut_AtLockoutEnd_ReturnsFalse()
    {
        DateTimeOffset lockoutEnd =
            new(
                2026,
                9,
                7,
                10,
                15,
                0,
                TimeSpan.Zero);

        var state =
            new UserLoginSecurityState(
                Guid.NewGuid(),
                failedLoginCount: 5,
                lockoutEndUtc:
                    lockoutEnd);

        Assert.False(
            state.IsLockedOut(
                lockoutEnd));
    }
}
