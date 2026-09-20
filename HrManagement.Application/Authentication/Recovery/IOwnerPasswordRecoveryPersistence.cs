namespace HrManagement.Application.Authentication.Recovery;

public interface IOwnerPasswordRecoveryPersistence
{
    Task<string?> GetRecoveryCodeHashAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task<bool> TryRecoverAsync(
        Guid accountId,
        string expectedRecoveryCodeHash,
        string newPasswordHash,
        string newRecoveryCodeHash,
        CancellationToken cancellationToken = default);
}
