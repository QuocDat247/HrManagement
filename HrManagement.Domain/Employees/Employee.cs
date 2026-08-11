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

    public DateOnly? TerminationDate { get; }

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
        EmployeeStatus status,
        DateOnly? terminationDate = null)
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

        if (terminationDate.HasValue
            && terminationDate.Value < hireDate)
        {
            throw new ArgumentException(
                "Ngày nghỉ việc không thể trước ngày vào làm.",
                nameof(terminationDate));
        }

        if (status != EmployeeStatus.Inactive
            && terminationDate.HasValue)
        {
            throw new ArgumentException(
                "Chỉ nhân viên ngừng hoạt động mới có ngày nghỉ việc.",
                nameof(terminationDate));
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
        TerminationDate = terminationDate;
    }

    public bool HasMissingProfileInformation =>
    Email is null
    || PhoneNumber is null
    || DateOfBirth is null;

    public bool RequiresProfileCompletion =>
        Status != EmployeeStatus.Inactive
        && HasMissingProfileInformation;
}
