using HrManagement.Domain.Authorization.Roles;

namespace HrManagement.Tests.Domain.Authorization.Roles;

public sealed class UserAccountRoleTests
{
    [Fact]
    public void
        Constructor_WithValidValues_CreatesAssignment()
    {
        Guid accountId =
            Guid.NewGuid();

        Guid roleId =
            Guid.NewGuid();

        var assignment =
            new UserAccountRole(
                accountId,
                roleId);

        Assert.Equal(
            accountId,
            assignment.AccountId);

        Assert.Equal(
            roleId,
            assignment.RoleId);
    }

    [Fact]
    public void
        Constructor_WithEmptyAccountId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new UserAccountRole(
                    Guid.Empty,
                    Guid.NewGuid()));
    }

    [Fact]
    public void
        Constructor_WithEmptyRoleId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new UserAccountRole(
                    Guid.NewGuid(),
                    Guid.Empty));
    }
}
