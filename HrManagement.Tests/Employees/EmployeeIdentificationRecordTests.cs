using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeIdentificationRecordTests
{
    [Fact]
    public void Constructor_WhenValid_NormalizesValues()
    {
        Guid id =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        DateOnly issueDate =
            new(
                2024,
                1,
                10);

        DateOnly expiryDate =
            new(
                2034,
                1,
                10);

        var record =
            new EmployeeIdentificationRecord(
                id,
                employeeId,
                EmployeeIdentificationType.Passport,
                "  P1234567  ",
                issueDate,
                expiryDate,
                "  Cục Quản lý xuất nhập cảnh  ",
                "  Hà Nội  ",
                "  Việt Nam  ");

        Assert.Equal(
            id,
            record.Id);

        Assert.Equal(
            employeeId,
            record.EmployeeId);

        Assert.Equal(
            EmployeeIdentificationType.Passport,
            record.Type);

        Assert.Equal(
            "P1234567",
            record.DocumentNumber);

        Assert.Equal(
            issueDate,
            record.IssueDate);

        Assert.Equal(
            expiryDate,
            record.ExpiryDate);

        Assert.Equal(
            "Cục Quản lý xuất nhập cảnh",
            record.IssuingAuthority);

        Assert.Equal(
            "Hà Nội",
            record.PlaceOfIssue);

        Assert.Equal(
            "Việt Nam",
            record.IssuingCountry);
    }

    [Fact]
    public void Constructor_WhenOptionalStringsBlank_NormalizesToNull()
    {
        var record =
            new EmployeeIdentificationRecord(
                Guid.NewGuid(),
                Guid.NewGuid(),
                EmployeeIdentificationType.NationalId,
                "012345678901",
                issuingAuthority: " ",
                placeOfIssue: " ",
                issuingCountry: " ");

        Assert.Null(
            record.IssuingAuthority);

        Assert.Null(
            record.PlaceOfIssue);

        Assert.Null(
            record.IssuingCountry);
    }

    [Fact]
    public void Constructor_WhenIdEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeIdentificationRecord(
                    Guid.Empty,
                    Guid.NewGuid(),
                    EmployeeIdentificationType.NationalId,
                    "012345678901"));
    }

    [Fact]
    public void Constructor_WhenEmployeeIdEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeIdentificationRecord(
                    Guid.NewGuid(),
                    Guid.Empty,
                    EmployeeIdentificationType.NationalId,
                    "012345678901"));
    }

    [Fact]
    public void Constructor_WhenTypeInvalid_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new EmployeeIdentificationRecord(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    (EmployeeIdentificationType)12345,
                    "ABC123"));
    }

    [Fact]
    public void Constructor_WhenDocumentNumberBlank_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeIdentificationRecord(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    EmployeeIdentificationType.Passport,
                    "   "));
    }

    [Fact]
    public void Constructor_WhenExpiryBeforeIssueDate_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new EmployeeIdentificationRecord(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    EmployeeIdentificationType.Passport,
                    "P1234567",
                    issueDate:
                        new DateOnly(
                            2030,
                            1,
                            1),
                    expiryDate:
                        new DateOnly(
                            2029,
                            12,
                            31)));
    }

    [Fact]
    public void Constructor_WhenDatesAreNotProvided_AllowsRecord()
    {
        var record =
            new EmployeeIdentificationRecord(
                Guid.NewGuid(),
                Guid.NewGuid(),
                EmployeeIdentificationType.NationalId,
                "012345678901");

        Assert.Null(
            record.IssueDate);

        Assert.Null(
            record.ExpiryDate);
    }
}
