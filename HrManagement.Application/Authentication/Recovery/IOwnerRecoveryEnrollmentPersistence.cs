using HrManagement.Domain.Authentication.Recovery;

namespace HrManagement.Application.Authentication.Recovery;

public interface IOwnerRecoveryEnrollmentPersistence
{
    Task<bool> ExistsAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task<bool> TryCreateAsync(
        OwnerRecoveryCredential credential,
        CancellationToken cancellationToken = default);
}
