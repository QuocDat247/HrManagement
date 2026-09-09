namespace HrManagement.Application.Authentication.Accounts;

public interface IAccountProfileUpdatePersistence
{
    Task<AccountProfileUpdatePersistenceResult> TryUpdateAsync(
        Guid accountId,
        string displayName,
        Guid? employeeId,
        CancellationToken cancellationToken = default);
}

public enum AccountProfileUpdatePersistenceResult
{
    Updated = 0,
    AccountNotFound = 1,
    EmployeeNotFound = 2,
    EmployeeAlreadyLinked = 3
}
