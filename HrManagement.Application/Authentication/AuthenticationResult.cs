namespace HrManagement.Application.Authentication;

public sealed record AuthenticationResult(
    bool IsSuccessful,
    string? ErrorMessage = null,
    bool MustChangePassword = false);
