namespace HrManagement.Domain.Employees;

public sealed class Employee
{
    public Guid Id { get; }

    public string EmployeeCode { get; }

    public string FullName { get; }

    public string? Email { get; }

    public string? PhoneNumber { get; }

    public DateOnly? DateOfBirth { get; }

    public DateOnly HireDate { get; }

    public string Department { get; }

    public string Position { get; }

    public EmployeeStatus Status { get; }

    public Employee(
        Guid id,
        string employeeCode,
        string fullName,
        string? email,
        string? phoneNumber,
        DateOnly? dateOfBirth,
        DateOnly hireDate,
        string department,
        string position,
        EmployeeStatus status)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Employee id must not be empty.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            throw new ArgumentException(
                "Employee code is required.",
                nameof(employeeCode));
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException(
                "Employee full name is required.",
                nameof(fullName));
        }

        if (hireDate == default)
        {
            throw new ArgumentException(
                "Hire date is required.",
                nameof(hireDate));
        }

        if (string.IsNullOrWhiteSpace(department))
        {
            throw new ArgumentException(
                "Department is required.",
                nameof(department));
        }

        if (string.IsNullOrWhiteSpace(position))
        {
            throw new ArgumentException(
                "Position is required.",
                nameof(position));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        Id = id;
        EmployeeCode = employeeCode.Trim();
        FullName = fullName.Trim();
        Email = string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim();

        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber)
            ? null
            : phoneNumber.Trim();

        DateOfBirth = dateOfBirth;
        HireDate = hireDate;
        Department = department.Trim();
        Position = position.Trim();
        Status = status;
    }
}
