using HrManagement.Domain.Authentication.Security;

namespace HrManagement.Application.Authentication.Security;

public interface IUserLoginSecurityStateRepository
{
    Task<UserLoginSecurityState?> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        UserLoginSecurityState state,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        UserLoginSecurityState state,
        CancellationToken cancellationToken = default);
}
