namespace HrManagement.Application.Authentication.Credentials;

public sealed record ResetAccountPasswordRequest(
    Guid AccountId,
    string NewPassword);
