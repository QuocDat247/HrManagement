using HrManagement.Domain.Authentication.Credentials;

namespace HrManagement.Application.Authentication.Credentials;

public interface IUserCredentialRepository
{
    Task<UserCredential?> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        UserCredential credential,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        UserCredential credential,
        CancellationToken cancellationToken = default);
}
