namespace HrManagement.Application.Authentication.Bootstrap;

public interface IInitialOwnerBootstrapService
{
    Task<bool> IsBootstrapRequiredAsync(
        CancellationToken cancellationToken = default);

    Task<OwnerBootstrapResult> CreateInitialOwnerAsync(
        string username,
        string displayName,
        string password,
        CancellationToken cancellationToken = default);
}
