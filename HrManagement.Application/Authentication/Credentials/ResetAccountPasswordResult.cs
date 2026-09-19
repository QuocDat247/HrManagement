namespace HrManagement.Application.Authentication.Credentials;

public sealed record ResetAccountPasswordResult(
    bool IsSuccessful,
    string? ErrorMessage = null);
