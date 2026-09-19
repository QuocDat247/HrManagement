namespace HrManagement.Application.Authentication.Recovery;

public interface IOwnerRecoveryEnrollmentService
{
    Task<bool> IsEnrollmentRequiredAsync(
        CancellationToken cancellationToken = default);

    string GenerateRecoveryCode();

    Task<OwnerRecoveryEnrollmentResult> EnrollAsync(
        string recoveryCode,
        CancellationToken cancellationToken = default);
}
