using HrManagement.Application.Authentication;
using HrManagement.Domain.Authentication.Credentials;

namespace HrManagement.Application.Authentication.Credentials;

public sealed class ChangePasswordService
    : IChangePasswordService
{
    private readonly ICurrentUserContext
        _currentUserContext;

    private readonly IUserCredentialRepository
        _credentialRepository;

    private readonly IPasswordHasher
        _passwordHasher;

    private readonly IPasswordPolicy
        _passwordPolicy;

    public ChangePasswordService(
        ICurrentUserContext currentUserContext,
        IUserCredentialRepository credentialRepository,
        IPasswordHasher passwordHasher,
        IPasswordPolicy passwordPolicy)
    {
        _currentUserContext =
            currentUserContext;

        _credentialRepository =
            credentialRepository;

        _passwordHasher =
            passwordHasher;

        _passwordPolicy =
            passwordPolicy;
    }

    public async Task<ChangePasswordResult> ChangeAsync(
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(currentPassword))
        {
            return Failure(
                "Vui lòng nhập mật khẩu hiện tại.");
        }

        if (string.IsNullOrEmpty(newPassword))
        {
            return Failure(
                "Vui lòng nhập mật khẩu mới.");
        }

        AuthenticatedUser? currentUser =
            _currentUserContext.CurrentUser;

        if (currentUser is null
            || !Guid.TryParse(
                currentUser.UserId,
                out Guid accountId))
        {
            return Failure(
                "Phiên đăng nhập không hợp lệ.");
        }

        UserCredential? credential =
            await _credentialRepository
                .GetByAccountIdAsync(
                    accountId,
                    cancellationToken);

        if (credential is null)
        {
            return Failure(
                "Không tìm thấy thông tin xác thực.");
        }

        PasswordVerificationResult verificationResult =
            _passwordHasher.VerifyPassword(
                currentPassword,
                credential.PasswordHash);

        if (verificationResult ==
            PasswordVerificationResult.Failed)
        {
            return Failure(
                "Mật khẩu hiện tại không đúng.");
        }

        PasswordPolicyResult policyResult =
            _passwordPolicy.Evaluate(
                newPassword,
                currentUser.Username);

        if (!policyResult.IsAccepted)
        {
            return Failure(
                GetPasswordPolicyMessage(
                    policyResult.Violation));
        }

        if (_passwordHasher.VerifyPassword(
                newPassword,
                credential.PasswordHash)
            != PasswordVerificationResult.Failed)
        {
            return Failure(
                "Mật khẩu mới phải khác mật khẩu hiện tại.");
        }

        string newPasswordHash =
            _passwordHasher.HashPassword(
                newPassword);

        await _credentialRepository.UpdateAsync(
            new UserCredential(
                accountId,
                newPasswordHash,
                mustChangePassword: false),
            cancellationToken);

        return new ChangePasswordResult(
            true);
    }

    private static ChangePasswordResult Failure(
        string message)
    {
        return new ChangePasswordResult(
            false,
            message);
    }

    private static string GetPasswordPolicyMessage(
        PasswordPolicyViolation violation)
    {
        return violation switch
        {
            PasswordPolicyViolation.TooShort =>
                "Mật khẩu mới quá ngắn.",

            PasswordPolicyViolation.TooLong =>
                "Mật khẩu mới quá dài.",

            PasswordPolicyViolation.Blocked =>
                "Mật khẩu mới này không được phép sử dụng.",

            _ =>
                "Mật khẩu mới không đáp ứng chính sách bảo mật."
        };
    }
}
