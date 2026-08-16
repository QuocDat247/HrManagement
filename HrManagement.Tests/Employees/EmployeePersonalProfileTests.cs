using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Tests.Employees;

public sealed class EmployeePersonalProfileTests
{
    [Fact]
    public void Constructor_WhenValid_NormalizesValues()
    {
        Guid employeeId =
            Guid.NewGuid();

        var profile =
            new EmployeePersonalProfile(
                employeeId,
                "  An  ",
                EmployeeGender.Male,
                "  Việt Nam  ",
                "  Hà Nội  ");

        Assert.Equal(
            employeeId,
            profile.EmployeeId);

        Assert.Equal(
            "An",
            profile.PreferredName);

        Assert.Equal(
            EmployeeGender.Male,
            profile.Gender);

        Assert.Equal(
            "Việt Nam",
            profile.Nationality);

        Assert.Equal(
            "Hà Nội",
            profile.PlaceOfBirth);
    }

    [Fact]
    public void Constructor_WhenOptionalValuesAreBlank_NormalizesToNull()
    {
        var profile =
            new EmployeePersonalProfile(
                Guid.NewGuid(),
                "   ",
                null,
                "",
                "  ");

        Assert.Null(
            profile.PreferredName);

        Assert.Null(
            profile.Gender);

        Assert.Null(
            profile.Nationality);

        Assert.Null(
            profile.PlaceOfBirth);
    }

    [Fact]
    public void Constructor_WhenOptionalValuesAreOmitted_AllowsEmptyProfile()
    {
        var profile =
            new EmployeePersonalProfile(
                Guid.NewGuid());

        Assert.Null(
            profile.PreferredName);

        Assert.Null(
            profile.Gender);

        Assert.Null(
            profile.Nationality);

        Assert.Null(
            profile.PlaceOfBirth);
    }

    [Fact]
    public void Constructor_WhenEmployeeIdIsEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeePersonalProfile(
                    Guid.Empty));
    }

    [Fact]
    public void Constructor_WhenGenderIsUndefined_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new EmployeePersonalProfile(
                    Guid.NewGuid(),
                    gender:
                        (EmployeeGender)123));
    }

    [Fact]
    public void EmployeeGender_HasStablePersistedValues()
    {
        Assert.Equal(
            1,
            (int)EmployeeGender.Male);

        Assert.Equal(
            2,
            (int)EmployeeGender.Female);

        Assert.Equal(
            99,
            (int)EmployeeGender.Other);
    }
}
