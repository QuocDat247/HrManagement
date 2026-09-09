namespace HrManagement.Application.Authentication.Accounts;

public sealed record UpdateAccountProfileResult(
    bool IsSuccessful,
    string? ErrorMessage = null);
