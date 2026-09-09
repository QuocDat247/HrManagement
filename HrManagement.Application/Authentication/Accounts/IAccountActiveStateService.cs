namespace HrManagement.Application.Authentication.Accounts;

public interface IAccountActiveStateService
{
    Task<SetAccountActiveStateResult> SetAsync(
        SetAccountActiveStateRequest request,
        CancellationToken cancellationToken = default);
}
