namespace HrManagement.Application.Authentication.Credentials;

public sealed record ChangePasswordResult(
    bool IsSuccessful,
    string? ErrorMessage = null);
