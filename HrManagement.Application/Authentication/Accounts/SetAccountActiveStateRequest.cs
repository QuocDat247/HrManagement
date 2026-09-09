namespace HrManagement.Application.Authentication.Accounts;

public sealed record SetAccountActiveStateRequest(
    Guid AccountId,
    bool IsActive);
