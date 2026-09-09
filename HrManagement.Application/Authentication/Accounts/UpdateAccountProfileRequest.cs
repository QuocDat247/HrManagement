namespace HrManagement.Application.Authentication.Accounts;

public sealed record UpdateAccountProfileRequest(
    Guid AccountId,
    string DisplayName,
    Guid? EmployeeId = null);
