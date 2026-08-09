using HrManagement.Domain.Employees;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesEmployee()
    {
        Guid id = Guid.NewGuid();

        var employee = new Employee(
            id,
            " EMP001 ",
            " Nguyễn Văn An ",
            " an@example.com ",
            " 0901234567 ",
            new DateOnly(1995, 5, 20),
            new DateOnly(2024, 1, 15),
            " Nhân sự ",
            " Chuyên viên ",
            EmployeeStatus.Active);

        Assert.Equal(id, employee.Id);
        Assert.Equal("EMP001", employee.EmployeeCode);
        Assert.Equal("Nguyễn Văn An", employee.FullName);
        Assert.Equal("an@example.com", employee.Email);
        Assert.Equal("0901234567", employee.PhoneNumber);
        Assert.Equal("Nhân sự", employee.Department);
        Assert.Equal("Chuyên viên", employee.Position);
        Assert.Equal(EmployeeStatus.Active, employee.Status);
    }

    [Fact]
    public void Constructor_WithEmptyEmployeeCode_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Employee(
                Guid.NewGuid(),
                "   ",
                "Nguyễn Văn An",
                null,
                null,
                null,
                new DateOnly(2024, 1, 15),
                "Nhân sự",
                "Chuyên viên",
                EmployeeStatus.Active));
    }

    [Fact]
    public void Constructor_WithEmptyFullName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Employee(
                Guid.NewGuid(),
                "EMP001",
                "",
                null,
                null,
                null,
                new DateOnly(2024, 1, 15),
                "Nhân sự",
                "Chuyên viên",
                EmployeeStatus.Active));
    }

    [Fact]
    public void Constructor_WithEmptyId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Employee(
                Guid.Empty,
                "EMP001",
                "Nguyễn Văn An",
                null,
                null,
                null,
                new DateOnly(2024, 1, 15),
                "Nhân sự",
                "Chuyên viên",
                EmployeeStatus.Active));
    }
}
