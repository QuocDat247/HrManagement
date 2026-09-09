using HrManagement.Domain.Authorization.Roles;

namespace HrManagement.Tests.Domain.Authorization.Roles;

public sealed class RoleTests
{
    [Fact]
    public void
        Constructor_WithValidValues_CreatesRole()
    {
        Guid id =
            Guid.NewGuid();

        var role =
            new Role(
                id,
                "Quản lý nhân sự",
                "Quản lý hồ sơ nhân viên.");

        Assert.Equal(
            id,
            role.Id);

        Assert.Equal(
            "Quản lý nhân sự",
            role.Name);

        Assert.Equal(
            "QUẢN LÝ NHÂN SỰ",
            role.NormalizedName);

        Assert.Equal(
            "Quản lý hồ sơ nhân viên.",
            role.Description);

        Assert.True(
            role.IsActive);
    }

    [Fact]
    public void
        Constructor_TrimsNameAndDescription()
    {
        var role =
            new Role(
                Guid.NewGuid(),
                "  Kế toán tiền lương  ",
                "  Xử lý nghiệp vụ tiền lương.  ");

        Assert.Equal(
            "Kế toán tiền lương",
            role.Name);

        Assert.Equal(
            "KẾ TOÁN TIỀN LƯƠNG",
            role.NormalizedName);

        Assert.Equal(
            "Xử lý nghiệp vụ tiền lương.",
            role.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void
        Constructor_WithBlankName_Throws(
            string name)
    {
        Assert.Throws<ArgumentException>(
            () =>
                new Role(
                    Guid.NewGuid(),
                    name));
    }

    [Fact]
    public void
        Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new Role(
                    Guid.Empty,
                    "Quản lý nhân sự"));
    }

    [Fact]
    public void
        Constructor_WithNameLongerThanMaximum_Throws()
    {
        string name =
            new(
                'R',
                101);

        Assert.Throws<ArgumentException>(
            () =>
                new Role(
                    Guid.NewGuid(),
                    name));
    }

    [Fact]
    public void
        Constructor_WithBlankDescription_NormalizesToNull()
    {
        var role =
            new Role(
                Guid.NewGuid(),
                "Quản lý chấm công",
                "   ");

        Assert.Null(
            role.Description);
    }

    [Fact]
    public void
        Constructor_WithDescriptionLongerThanMaximum_Throws()
    {
        string description =
            new(
                'D',
                501);

        Assert.Throws<ArgumentException>(
            () =>
                new Role(
                    Guid.NewGuid(),
                    "Quản lý nhân sự",
                    description));
    }

    [Fact]
    public void
        Constructor_WithInactiveRole_PreservesInactiveState()
    {
        var role =
            new Role(
                Guid.NewGuid(),
                "Vai trò ngừng sử dụng",
                isActive: false);

        Assert.False(
            role.IsActive);
    }

    [Fact]
    public void
        UpdateDetails_WithValidValues_ChangesDetailsOnly()
    {
        var role =
            new Role(
                Guid.NewGuid(),
                "Tên cũ",
                "Mô tả cũ",
                isActive:
                    false);

        role.UpdateDetails(
            "  Quản lý nhân sự  ",
            "  Quản lý hồ sơ.  ");

        Assert.Equal(
            "Quản lý nhân sự",
            role.Name);

        Assert.Equal(
            "QUẢN LÝ NHÂN SỰ",
            role.NormalizedName);

        Assert.Equal(
            "Quản lý hồ sơ.",
            role.Description);

        Assert.False(
            role.IsActive);
    }

    [Fact]
    public void
        UpdateDetails_WithBlankDescription_NormalizesToNull()
    {
        var role =
            new Role(
                Guid.NewGuid(),
                "Quản lý");

        role.UpdateDetails(
            "Quản lý mới",
            "   ");

        Assert.Null(
            role.Description);
    }

    [Fact]
    public void
        UpdateDetails_WithBlankName_Throws()
    {
        var role =
            new Role(
                Guid.NewGuid(),
                "Quản lý");

        Assert.Throws<ArgumentException>(
            () =>
                role.UpdateDetails(
                    "   "));
    }

    [Fact]
    public void
        SetActive_CanDisableAndEnableWithoutChangingDetails()
    {
        Guid roleId =
            Guid.NewGuid();

        var role =
            new Role(
                roleId,
                "Quản lý nhân sự",
                "Quản lý hồ sơ.");

        role.SetActive(
            false);

        Assert.False(
            role.IsActive);

        Assert.Equal(
            roleId,
            role.Id);

        Assert.Equal(
            "Quản lý nhân sự",
            role.Name);

        Assert.Equal(
            "QUẢN LÝ NHÂN SỰ",
            role.NormalizedName);

        Assert.Equal(
            "Quản lý hồ sơ.",
            role.Description);

        role.SetActive(
            true);

        Assert.True(
            role.IsActive);

        Assert.Equal(
            "Quản lý nhân sự",
            role.Name);
    }
}
