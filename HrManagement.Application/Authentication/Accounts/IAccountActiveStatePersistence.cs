namespace HrManagement.Application.Authentication.Accounts;

public interface IAccountActiveStatePersistence
{
    Task<AccountActiveStatePersistenceResult> TrySetAsync(
        Guid accountId,
        bool isActive,
        CancellationToken cancellationToken = default);
}

public enum AccountActiveStatePersistenceResult
{
    Updated = 0,
    Unchanged = 1,
    AccountNotFound = 2,
    LastActiveOwner = 3
}
