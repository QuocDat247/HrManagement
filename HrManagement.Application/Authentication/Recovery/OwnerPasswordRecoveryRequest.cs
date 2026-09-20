namespace HrManagement.Application.Authentication.Recovery;

public sealed record OwnerPasswordRecoveryRequest(
    string Username,
    string RecoveryCode,
    string NewPassword);
