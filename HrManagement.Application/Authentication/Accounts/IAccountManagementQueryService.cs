namespace HrManagement.Application.Authentication.Accounts;

public interface IAccountManagementQueryService
{
    Task<AccountManagementSnapshot> GetAsync(
        CancellationToken cancellationToken = default);
}
