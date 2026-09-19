using HrManagement.Application.Authentication.Accounts;
using HrManagement.Domain.Authentication.Accounts;

namespace HrManagement.Application.Authentication.Credentials;

public sealed class AccountPasswordResetService
    : IAccountPasswordResetService
{
    private readonly ICurrentUserContext
        _currentUserContext;

    private readonly IUserAccountRepository
        _accountRepository;

    private readonly IPasswordPolicy
        _passwordPolicy;

    private readonly IPasswordHasher
        _passwordHasher;

    private readonly IAccountPasswordResetPersistence
        _persistence;

    public AccountPasswordResetService(
        ICurrentUserContext currentUserContext,
        IUserAccountRepository accountRepository,
        IPasswordPolicy passwordPolicy,
        IPasswordHasher passwordHasher,
        IAccountPasswordResetPersistence persistence)
    {
        _currentUserContext =
            currentUserContext;

        _accountRepository =
            accountRepository;

        _passwordPolicy =
            passwordPolicy;

        _passwordHasher =
            passwordHasher;

        _persistence =
            persistence;
    }

    public async Task<ResetAccountPasswordResult> ResetAsync(
        ResetAccountPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        cancellationToken
            .ThrowIfCancellationRequested();

        if (request.AccountId == Guid.Empty)
        {
            return Failure(
                "Tài khoản không hợp lệ.");
        }

        if (string.IsNullOrEmpty(
                request.NewPassword))
        {
            return Failure(
                "Vui lòng nhập mật khẩu tạm mới.");
        }

        AuthenticatedUser? currentUser =
            _currentUserContext.CurrentUser;

        if (!_currentUserContext.IsAuthenticated
            || currentUser is null
            || !Guid.TryParse(
                currentUser.UserId,
                out Guid currentAccountId))
        {
            return Failure(
                "Chỉ Owner mới có thể đặt lại mật khẩu tài khoản.");
        }

        UserAccount? currentAccount =
            await _accountRepository
                .GetByIdAsync(
                    currentAccountId,
                    cancellationToken);

        if (currentAccount is null
            || !currentAccount.IsActive
            || currentAccount.Kind !=
                UserAccountKind.Owner)
        {
            return Failure(
                "Chỉ Owner mới có thể đặt lại mật khẩu tài khoản.");
        }

        UserAccount? targetAccount =
            await _accountRepository
                .GetByIdAsync(
                    request.AccountId,
                    cancellationToken);

        if (targetAccount is null)
        {
            return Failure(
                "Không tìm thấy tài khoản.");
        }

        if (targetAccount.Kind !=
            UserAccountKind.Standard)
        {
            return Failure(
                "Chỉ có thể đặt lại mật khẩu cho tài khoản Standard.");
        }

        PasswordPolicyResult policyResult =
            _passwordPolicy.Evaluate(
                request.NewPassword,
                targetAccount.Username);

        if (!policyResult.IsAccepted)
        {
            return Failure(
                GetPasswordPolicyMessage(
                    policyResult.Violation));
        }

        string passwordHash =
            _passwordHasher.HashPassword(
                request.NewPassword);

        bool reset =
            await _persistence
                .TryResetAsync(
                    targetAccount.Id,
                    passwordHash,
                    cancellationToken);

        if (!reset)
        {
            return Failure(
                "Không tìm thấy thông tin xác thực của tài khoản.");
        }

        return new ResetAccountPasswordResult(
            true);
    }

    private static ResetAccountPasswordResult Failure(
        string message)
    {
        return new ResetAccountPasswordResult(
            false,
            message);
    }

    private static string GetPasswordPolicyMessage(
        PasswordPolicyViolation violation)
    {
        return violation switch
        {
            PasswordPolicyViolation.TooShort =>
                "Mật khẩu tạm mới quá ngắn.",

            PasswordPolicyViolation.TooLong =>
                "Mật khẩu tạm mới quá dài.",

            PasswordPolicyViolation.Blocked =>
                "Mật khẩu tạm này không được phép sử dụng.",

            _ =>
                "Mật khẩu tạm không đáp ứng chính sách bảo mật."
        };
    }
}
