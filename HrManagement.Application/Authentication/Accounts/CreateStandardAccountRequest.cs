namespace HrManagement.Application.Authentication.Accounts;

public sealed record CreateStandardAccountRequest(
    string Username,
    string DisplayName,
    string Password,
    Guid? EmployeeId = null);
