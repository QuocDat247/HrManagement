using HrManagement.Domain.Authorization.Permissions;
using HrManagement.Domain.Authorization.Roles;

namespace HrManagement.Tests.Domain.Authorization.Roles;

public sealed class RolePermissionTests
{
    [Fact]
    public void
        Constructor_WithValidValues_CreatesRolePermission()
    {
        Guid roleId =
            Guid.NewGuid();

        var rolePermission =
            new RolePermission(
                roleId,
                PermissionCodes.EmployeeView);

        Assert.Equal(
            roleId,
            rolePermission.RoleId);

        Assert.Equal(
            PermissionCodes.EmployeeView,
            rolePermission.PermissionCode);
    }

    [Fact]
    public void
        Constructor_TrimsPermissionCode()
    {
        var rolePermission =
            new RolePermission(
                Guid.NewGuid(),
                $"  {PermissionCodes.PayrollView}  ");

        Assert.Equal(
            PermissionCodes.PayrollView,
            rolePermission.PermissionCode);
    }

    [Fact]
    public void
        Constructor_WithEmptyRoleId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new RolePermission(
                    Guid.Empty,
                    PermissionCodes.EmployeeView));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Unknown.Permission")]
    [InlineData("employee.view")]
    public void
        Constructor_WithInvalidPermissionCode_Throws(
            string permissionCode)
    {
        Assert.Throws<ArgumentException>(
            () =>
                new RolePermission(
                    Guid.NewGuid(),
                    permissionCode));
    }
}
