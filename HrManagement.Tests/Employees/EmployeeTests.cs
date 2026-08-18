using HrManagement.Domain.Employees;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeTests
{
    [Fact]
    public void Constructor_WhenInactiveWithTerminationDate_SetsTerminationDate()
    {
        DateOnly terminationDate =
            new DateOnly(2026, 7, 31);

        var employee = new Employee(
            Guid.NewGuid(),
            "EMP001",
            "Nguyễn Văn An",
            "an@example.com",
            "0901000001",
            new DateOnly(1995, 5, 20),
            new DateOnly(2022, 3, 1),
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Inactive,
            terminationDate: terminationDate);

        Assert.Equal(
            terminationDate,
            employee.TerminationDate);
    }

    [Fact]
    public void Constructor_WhenTerminationDateIsBeforeHireDate_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new Employee(
                Guid.NewGuid(),
                "EMP002",
                "Trần Thị Bình",
                "binh@example.com",
                "0901000002",
                new DateOnly(1994, 7, 10),
                new DateOnly(2024, 1, 1),
                "Kế toán",
                "Kế toán viên",
                EmployeeStatus.Inactive,
                terminationDate:
                    new DateOnly(2023, 12, 31)));
    }

    [Fact]
    public void Constructor_WhenActiveHasTerminationDate_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new Employee(
                Guid.NewGuid(),
                "EMP003",
                "Lê Minh Châu",
                "chau@example.com",
                "0901000003",
                new DateOnly(1996, 3, 15),
                new DateOnly(2023, 2, 10),
                "Công nghệ thông tin",
                "Lập trình viên",
                EmployeeStatus.Active,
                terminationDate:
                    new DateOnly(2026, 8, 1)));
    }

    [Fact]
    public void Constructor_WhenInactiveHasUnknownTerminationDate_AllowsNull()
    {
        var employee = new Employee(
            Guid.NewGuid(),
            "EMP004",
            "Võ Thu Hà",
            null,
            null,
            null,
            new DateOnly(2019, 6, 20),
            "Hành chính",
            "Chuyên viên hành chính",
            EmployeeStatus.Inactive);

        Assert.Null(employee.TerminationDate);
    }
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

    [Fact]
    public void Constructor_WhenActiveEmployeeHasMissingEmail_AllowsNullEmail()
    {
        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP001",
                "Nguyễn Văn An",
                null,
                "0901000001",
                new DateOnly(1995, 5, 20),
                new DateOnly(2022, 3, 1),
                "Nhân sự",
                "Chuyên viên",
                EmployeeStatus.Active);

        Assert.Null(
            employee.Email);

        Assert.Equal(
            EmployeeStatus.Active,
            employee.Status);
    }

    [Fact]
    public void Constructor_WhenActiveEmployeeHasMissingPhoneNumber_AllowsNullPhoneNumber()
    {
        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP002",
                "Trần Thị Bình",
                "binh@example.com",
                null,
                new DateOnly(1994, 7, 10),
                new DateOnly(2023, 1, 1),
                "Kế toán",
                "Kế toán viên",
                EmployeeStatus.Active);

        Assert.Null(
            employee.PhoneNumber);

        Assert.Equal(
            EmployeeStatus.Active,
            employee.Status);
    }

    [Fact]
    public void Constructor_WhenActiveEmployeeHasMissingDateOfBirth_AllowsNullDateOfBirth()
    {
        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP003",
                "Võ Thu Hà",
                "ha@example.com",
                "0901000003",
                null,
                new DateOnly(2019, 6, 20),
                "Hành chính",
                "Chuyên viên hành chính",
                EmployeeStatus.Active);

        Assert.Null(
            employee.DateOfBirth);

        Assert.Equal(
            EmployeeStatus.Active,
            employee.Status);
    }

    [Fact]
    public void Constructor_WithOrganizationReferences_PreservesIdentifiers()
    {
        Guid departmentId =
            Guid.NewGuid();

        Guid positionId =
            Guid.NewGuid();

        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-ORG-001",
                "Nguyễn Văn An",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "Công nghệ thông tin",
                "Lập trình viên",
                EmployeeStatus.Active,
                departmentId: departmentId,
                positionId: positionId);

        Assert.Equal(
            departmentId,
            employee.DepartmentId);

        Assert.Equal(
            positionId,
            employee.PositionId);
    }

    [Fact]
    public void Constructor_WithoutOrganizationReferences_AllowsLegacyEmployee()
    {
        var employee =
            new Employee(
                Guid.NewGuid(),
                "EMP-ORG-002",
                "Legacy Employee",
                null,
                null,
                null,
                new DateOnly(2020, 1, 1),
                "IT",
                "Developer",
                EmployeeStatus.Active);

        Assert.Null(
            employee.DepartmentId);

        Assert.Null(
            employee.PositionId);
    }

    [Fact]
    public void Constructor_WithEmptyDepartmentId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new Employee(
                Guid.NewGuid(),
                "EMP-ORG-003",
                "Employee",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "IT",
                "Developer",
                EmployeeStatus.Active,
                departmentId: Guid.Empty));
    }

    [Fact]
    public void Constructor_WithEmptyPositionId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new Employee(
                Guid.NewGuid(),
                "EMP-ORG-004",
                "Employee",
                null,
                null,
                null,
                new DateOnly(2024, 1, 1),
                "IT",
                "Developer",
                EmployeeStatus.Active,
                positionId: Guid.Empty));
    }
}
