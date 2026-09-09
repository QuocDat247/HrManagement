namespace HrManagement.Application.Authentication.Accounts;

public interface IStandardAccountCreationService
{
    Task<CreateStandardAccountResult> CreateAsync(
        CreateStandardAccountRequest request,
        CancellationToken cancellationToken = default);
}
