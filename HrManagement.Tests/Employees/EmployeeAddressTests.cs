using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeAddressTests
{
    [Fact]
    public void Constructor_WhenValid_NormalizesValues()
    {
        Guid id =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        var address =
            new EmployeeAddress(
                id,
                employeeId,
                EmployeeAddressType.Current,
                "  123 Nguyễn Trãi  ",
                "  Phường Bến Thành  ",
                "  Quận 1  ",
                "  TP. Hồ Chí Minh  ",
                "  Việt Nam  ",
                "  700000  ");

        Assert.Equal(
            id,
            address.Id);

        Assert.Equal(
            employeeId,
            address.EmployeeId);

        Assert.Equal(
            EmployeeAddressType.Current,
            address.Type);

        Assert.Equal(
            "123 Nguyễn Trãi",
            address.AddressLine);

        Assert.Equal(
            "Phường Bến Thành",
            address.Ward);

        Assert.Equal(
            "Quận 1",
            address.District);

        Assert.Equal(
            "TP. Hồ Chí Minh",
            address.Province);

        Assert.Equal(
            "Việt Nam",
            address.Country);

        Assert.Equal(
            "700000",
            address.PostalCode);
    }

    [Fact]
    public void Constructor_WhenOptionalValuesAreBlank_NormalizesToNull()
    {
        var address =
            new EmployeeAddress(
                Guid.NewGuid(),
                Guid.NewGuid(),
                EmployeeAddressType.Permanent,
                "123 Test",
                " ",
                "",
                null,
                "Việt Nam",
                "   ");

        Assert.Null(
            address.Ward);

        Assert.Null(
            address.District);

        Assert.Null(
            address.Province);

        Assert.Null(
            address.PostalCode);
    }

    [Fact]
    public void Constructor_WhenAddressLineIsBlank_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeAddress(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    EmployeeAddressType.Current,
                    "   "));
    }

    [Fact]
    public void Constructor_WhenEmployeeIdIsEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeAddress(
                    Guid.NewGuid(),
                    Guid.Empty,
                    EmployeeAddressType.Current,
                    "123 Test"));
    }

    [Fact]
    public void Constructor_WhenIdIsEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeAddress(
                    Guid.Empty,
                    Guid.NewGuid(),
                    EmployeeAddressType.Current,
                    "123 Test"));
    }

    [Fact]
    public void Constructor_WhenTypeIsUndefined_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new EmployeeAddress(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    (EmployeeAddressType)123,
                    "123 Test"));
    }

    [Fact]
    public void Constructor_WhenCountryIsBlank_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeAddress(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    EmployeeAddressType.Current,
                    "123 Test",
                    country: " "));
    }
}
