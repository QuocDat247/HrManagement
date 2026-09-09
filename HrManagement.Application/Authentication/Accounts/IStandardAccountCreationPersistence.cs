using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Credentials;
using HrManagement.Domain.Authentication.Security;

namespace HrManagement.Application.Authentication.Accounts;

public interface IStandardAccountCreationPersistence
{
    Task<StandardAccountCreationPersistenceResult> TryCreateAsync(
        UserAccount account,
        UserCredential credential,
        UserLoginSecurityState securityState,
        CancellationToken cancellationToken = default);
}

public enum StandardAccountCreationPersistenceResult
{
    Created = 0,
    UsernameAlreadyExists = 1,
    EmployeeNotFound = 2,
    EmployeeAlreadyLinked = 3
}
