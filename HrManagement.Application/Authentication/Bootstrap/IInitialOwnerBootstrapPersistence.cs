using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Credentials;
using HrManagement.Domain.Authentication.Security;

namespace HrManagement.Application.Authentication.Bootstrap;

public interface IInitialOwnerBootstrapPersistence
{
    Task<bool> TryCreateAsync(
        UserAccount account,
        UserCredential credential,
        UserLoginSecurityState securityState,
        CancellationToken cancellationToken = default);
}
