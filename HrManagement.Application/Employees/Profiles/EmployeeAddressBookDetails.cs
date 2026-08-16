namespace HrManagement.Application.Employees.Profiles;

public sealed record EmployeeAddressBookDetails(
    Guid EmployeeId,
    EmployeeAddressDetails? PermanentAddress,
    EmployeeAddressDetails? CurrentAddress);
