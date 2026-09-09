namespace HrManagement.Application.Authentication.Accounts;

public sealed record SetAccountActiveStateResult(
    bool IsSuccessful,
    string? ErrorMessage = null);
