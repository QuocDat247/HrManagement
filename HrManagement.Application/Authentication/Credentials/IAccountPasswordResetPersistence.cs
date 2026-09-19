namespace HrManagement.Application.Authentication.Credentials;

public interface IAccountPasswordResetPersistence
{
    Task<bool> TryResetAsync(
        Guid accountId,
        string passwordHash,
        CancellationToken cancellationToken = default);
}
