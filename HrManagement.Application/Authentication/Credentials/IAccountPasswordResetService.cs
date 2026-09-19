namespace HrManagement.Application.Authentication.Credentials;

public interface IAccountPasswordResetService
{
    Task<ResetAccountPasswordResult> ResetAsync(
        ResetAccountPasswordRequest request,
        CancellationToken cancellationToken = default);
}
