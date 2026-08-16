using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeEmergencyContactTests
{
    [Fact]
    public void Constructor_WhenValid_NormalizesValues()
    {
        Guid id =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        var contact =
            new EmployeeEmergencyContact(
                id,
                employeeId,
                "  Nguyễn Văn Bình  ",
                "  Cha  ",
                "  +84 901 234 567  ",
                "  binh@example.com  ",
                isPrimary: true);

        Assert.Equal(
            id,
            contact.Id);

        Assert.Equal(
            employeeId,
            contact.EmployeeId);

        Assert.Equal(
            "Nguyễn Văn Bình",
            contact.FullName);

        Assert.Equal(
            "Cha",
            contact.Relationship);

        Assert.Equal(
            "+84 901 234 567",
            contact.PhoneNumber);

        Assert.Equal(
            "binh@example.com",
            contact.Email);

        Assert.True(
            contact.IsPrimary);
    }

    [Fact]
    public void Constructor_WhenEmailIsBlank_NormalizesToNull()
    {
        var contact =
            new EmployeeEmergencyContact(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Nguyễn Văn Bình",
                "Cha",
                "0901234567",
                "   ");

        Assert.Null(
            contact.Email);
    }

    [Fact]
    public void Constructor_WhenIdIsEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeEmergencyContact(
                    Guid.Empty,
                    Guid.NewGuid(),
                    "Nguyễn Văn Bình",
                    "Cha",
                    "0901234567"));
    }

    [Fact]
    public void Constructor_WhenEmployeeIdIsEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeEmergencyContact(
                    Guid.NewGuid(),
                    Guid.Empty,
                    "Nguyễn Văn Bình",
                    "Cha",
                    "0901234567"));
    }

    [Fact]
    public void Constructor_WhenFullNameIsBlank_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeEmergencyContact(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "   ",
                    "Cha",
                    "0901234567"));
    }

    [Fact]
    public void Constructor_WhenRelationshipIsBlank_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeEmergencyContact(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Nguyễn Văn Bình",
                    "   ",
                    "0901234567"));
    }

    [Fact]
    public void Constructor_WhenPhoneNumberIsBlank_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeEmergencyContact(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Nguyễn Văn Bình",
                    "Cha",
                    "   "));
    }

    [Fact]
    public void Constructor_WhenPrimaryIsNotSpecified_DefaultsToFalse()
    {
        var contact =
            new EmployeeEmergencyContact(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Nguyễn Văn Bình",
                "Cha",
                "0901234567");

        Assert.False(
            contact.IsPrimary);
    }
}
