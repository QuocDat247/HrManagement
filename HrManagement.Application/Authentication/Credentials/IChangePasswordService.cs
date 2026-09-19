namespace HrManagement.Application.Authentication.Credentials;

public interface IChangePasswordService
{
    Task<ChangePasswordResult> ChangeAsync(
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);
}
