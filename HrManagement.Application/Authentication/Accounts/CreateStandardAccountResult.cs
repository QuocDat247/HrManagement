namespace HrManagement.Application.Authentication.Accounts;

public sealed record CreateStandardAccountResult(
    bool IsSuccessful,
    Guid? AccountId = null,
    string? ErrorMessage = null);
