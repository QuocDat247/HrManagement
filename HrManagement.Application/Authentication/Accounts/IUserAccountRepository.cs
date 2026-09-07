using HrManagement.Domain.Authentication.Accounts;

namespace HrManagement.Application.Authentication.Accounts;

public interface IUserAccountRepository
{
    Task<IReadOnlyList<UserAccount>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<UserAccount?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<UserAccount?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        UserAccount account,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        UserAccount account,
        CancellationToken cancellationToken = default);
}
