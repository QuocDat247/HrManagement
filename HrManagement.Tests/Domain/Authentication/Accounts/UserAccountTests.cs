using HrManagement.Domain.Authentication.Accounts;

namespace HrManagement.Tests.Domain.Authentication.Accounts;

public sealed class UserAccountTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesAccount()
    {
        Guid id =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        var account =
            new UserAccount(
                id,
                " owner.admin ",
                " Chủ doanh nghiệp ",
                UserAccountKind.Owner,
                employeeId);

        Assert.Equal(
            id,
            account.Id);

        Assert.Equal(
            "owner.admin",
            account.Username);

        Assert.Equal(
            "OWNER.ADMIN",
            account.NormalizedUsername);

        Assert.Equal(
            "Chủ doanh nghiệp",
            account.DisplayName);

        Assert.Equal(
            employeeId,
            account.EmployeeId);

        Assert.Equal(
            UserAccountKind.Owner,
            account.Kind);

        Assert.True(
            account.IsActive);
    }

    [Fact]
    public void Constructor_WithoutEmployee_AllowsAccount()
    {
        var account =
            new UserAccount(
                Guid.NewGuid(),
                "owner",
                "Chủ doanh nghiệp",
                UserAccountKind.Owner);

        Assert.Null(
            account.EmployeeId);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new UserAccount(
                Guid.Empty,
                "owner",
                "Chủ doanh nghiệp",
                UserAccountKind.Owner));
    }

    [Fact]
    public void Constructor_WithBlankUsername_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new UserAccount(
                Guid.NewGuid(),
                "   ",
                "Chủ doanh nghiệp",
                UserAccountKind.Owner));
    }

    [Fact]
    public void Constructor_WithWhitespaceInsideUsername_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new UserAccount(
                Guid.NewGuid(),
                "owner admin",
                "Chủ doanh nghiệp",
                UserAccountKind.Owner));
    }

    [Fact]
    public void Constructor_WithBlankDisplayName_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new UserAccount(
                Guid.NewGuid(),
                "owner",
                "   ",
                UserAccountKind.Owner));
    }

    [Fact]
    public void Constructor_WithEmptyEmployeeId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new UserAccount(
                Guid.NewGuid(),
                "owner",
                "Chủ doanh nghiệp",
                UserAccountKind.Owner,
                Guid.Empty));
    }

    [Fact]
    public void Constructor_WithInvalidKind_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new UserAccount(
                Guid.NewGuid(),
                "owner",
                "Chủ doanh nghiệp",
                (UserAccountKind)999));
    }

    [Fact]
    public void Constructor_WithInactiveAccount_PreservesState()
    {
        var account =
            new UserAccount(
                Guid.NewGuid(),
                "accounting",
                "Kế toán",
                UserAccountKind.Standard,
                isActive: false);

        Assert.False(
            account.IsActive);
    }

    [Fact]
    public void
        UpdateProfile_WithValidValues_ChangesOnlyProfileFields()
    {
        Guid employeeId =
            Guid.NewGuid();

        var account =
            new UserAccount(
                Guid.NewGuid(),
                "manager",
                "Tên cũ",
                UserAccountKind.Standard,
                isActive:
                    false);

        account.UpdateProfile(
            "  Tên mới  ",
            employeeId);

        Assert.Equal(
            "Tên mới",
            account.DisplayName);

        Assert.Equal(
            employeeId,
            account.EmployeeId);

        Assert.Equal(
            "manager",
            account.Username);

        Assert.Equal(
            UserAccountKind.Standard,
            account.Kind);

        Assert.False(
            account.IsActive);
    }

    [Fact]
    public void
        UpdateProfile_WithBlankDisplayName_Throws()
    {
        var account =
            new UserAccount(
                Guid.NewGuid(),
                "manager",
                "Quản lý",
                UserAccountKind.Standard);

        Assert.Throws<ArgumentException>(
            () =>
                account.UpdateProfile(
                    "   "));
    }

    [Fact]
    public void
        UpdateProfile_WithEmptyEmployeeId_Throws()
    {
        var account =
            new UserAccount(
                Guid.NewGuid(),
                "manager",
                "Quản lý",
                UserAccountKind.Standard);

        Assert.Throws<ArgumentException>(
            () =>
                account.UpdateProfile(
                    "Quản lý mới",
                    Guid.Empty));
    }

    [Fact]
    public void
        SetActive_CanDisableAndEnableWithoutChangingIdentity()
    {
        Guid employeeId =
            Guid.NewGuid();

        var account =
            new UserAccount(
                Guid.NewGuid(),
                "manager",
                "Quản lý",
                UserAccountKind.Standard,
                employeeId);

        account.SetActive(
            false);

        Assert.False(
            account.IsActive);

        Assert.Equal(
            "manager",
            account.Username);

        Assert.Equal(
            "Quản lý",
            account.DisplayName);

        Assert.Equal(
            employeeId,
            account.EmployeeId);

        Assert.Equal(
            UserAccountKind.Standard,
            account.Kind);

        account.SetActive(
            true);

        Assert.True(
            account.IsActive);

        Assert.Equal(
            UserAccountKind.Standard,
            account.Kind);
    }
}
