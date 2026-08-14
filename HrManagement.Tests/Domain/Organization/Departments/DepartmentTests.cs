using HrManagement.Domain.Organization.Departments;

namespace HrManagement.Tests.Domain.Organization.Departments;

public sealed class DepartmentTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesDepartment()
    {
        Guid id =
            Guid.NewGuid();

        var department =
            new Department(
                id,
                " it ",
                " Phòng Công nghệ thông tin ");

        Assert.Equal(
            id,
            department.Id);

        Assert.Equal(
            "IT",
            department.Code);

        Assert.Equal(
            "Phòng Công nghệ thông tin",
            department.Name);

        Assert.True(
            department.IsActive);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new Department(
                Guid.Empty,
                "IT",
                "Công nghệ thông tin"));
    }

    [Fact]
    public void Constructor_WithBlankCode_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new Department(
                Guid.NewGuid(),
                "   ",
                "Công nghệ thông tin"));
    }

    [Fact]
    public void Constructor_WithBlankName_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new Department(
                Guid.NewGuid(),
                "IT",
                "   "));
    }
}
