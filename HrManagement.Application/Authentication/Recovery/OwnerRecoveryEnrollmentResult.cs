namespace HrManagement.Application.Authentication.Recovery;

public sealed record OwnerRecoveryEnrollmentResult(
    bool IsSuccessful,
    string? ErrorMessage = null);
