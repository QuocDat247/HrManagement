namespace HrManagement.Application.Authentication.Accounts;

public interface IAccountProfileUpdateService
{
    Task<UpdateAccountProfileResult> UpdateAsync(
        UpdateAccountProfileRequest request,
        CancellationToken cancellationToken = default);
}
