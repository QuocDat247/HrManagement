namespace HrManagement.Application.Authentication.Recovery;

public sealed record OwnerPasswordRecoveryResult(
    bool IsSuccessful,
    string? ErrorMessage = null,
    string? NewRecoveryCode = null);
