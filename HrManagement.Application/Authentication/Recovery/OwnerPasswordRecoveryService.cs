using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authentication.Credentials;
using HrManagement.Domain.Authentication.Accounts;

namespace HrManagement.Application.Authentication.Recovery;

public sealed class OwnerPasswordRecoveryService
    : IOwnerPasswordRecoveryService
{
    private const string GenericRecoveryFailureMessage =
        "Không thể khôi phục tài khoản Owner bằng thông tin đã cung cấp.";

    private readonly IUserAccountRepository
        _accountRepository;

    private readonly IPasswordPolicy
        _passwordPolicy;

    private readonly IPasswordHasher
        _passwordHasher;

    private readonly IRecoveryCodeHasher
        _recoveryCodeHasher;

    private readonly IRecoveryCodeGenerator
        _recoveryCodeGenerator;

    private readonly IOwnerPasswordRecoveryPersistence
        _persistence;

    public OwnerPasswordRecoveryService(
        IUserAccountRepository accountRepository,
        IPasswordPolicy passwordPolicy,
        IPasswordHasher passwordHasher,
        IRecoveryCodeHasher recoveryCodeHasher,
        IRecoveryCodeGenerator recoveryCodeGenerator,
        IOwnerPasswordRecoveryPersistence persistence)
    {
        _accountRepository =
            accountRepository;

        _passwordPolicy =
            passwordPolicy;

        _passwordHasher =
            passwordHasher;

        _recoveryCodeHasher =
            recoveryCodeHasher;

        _recoveryCodeGenerator =
            recoveryCodeGenerator;

        _persistence =
            persistence;
    }

    public async Task<OwnerPasswordRecoveryResult>
        RecoverAsync(
            OwnerPasswordRecoveryRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        cancellationToken
            .ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(
                request.Username)
            || string.IsNullOrWhiteSpace(
                request.RecoveryCode))
        {
            return Failure(
                GenericRecoveryFailureMessage);
        }

        if (string.IsNullOrEmpty(
                request.NewPassword))
        {
            return Failure(
                "Vui lòng nhập mật khẩu mới.");
        }

        string username =
            request.Username.Trim();

        PasswordPolicyResult policyResult =
            _passwordPolicy.Evaluate(
                request.NewPassword,
                username);

        if (!policyResult.IsAccepted)
        {
            return Failure(
                GetPasswordPolicyMessage(
                    policyResult.Violation));
        }

        if (RecoveryCodeFormat.Normalize(
                request.RecoveryCode) is null)
        {
            return Failure(
                GenericRecoveryFailureMessage);
        }

        UserAccount? account =
            await _accountRepository
                .GetByUsernameAsync(
                    username,
                    cancellationToken);

        if (account is null
            || !account.IsActive
            || account.Kind !=
                UserAccountKind.Owner)
        {
            return Failure(
                GenericRecoveryFailureMessage);
        }

        string? currentRecoveryCodeHash =
            await _persistence
                .GetRecoveryCodeHashAsync(
                    account.Id,
                    cancellationToken);

        if (string.IsNullOrWhiteSpace(
                currentRecoveryCodeHash))
        {
            return Failure(
                GenericRecoveryFailureMessage);
        }

        bool recoveryCodeValid =
            _recoveryCodeHasher.Verify(
                request.RecoveryCode,
                currentRecoveryCodeHash);

        if (!recoveryCodeValid)
        {
            return Failure(
                GenericRecoveryFailureMessage);
        }

        string newPasswordHash =
            _passwordHasher.HashPassword(
                request.NewPassword);

        string newRecoveryCode =
            _recoveryCodeGenerator.Generate();

        string newRecoveryCodeHash =
            _recoveryCodeHasher.Hash(
                newRecoveryCode);

        bool recovered =
            await _persistence
                .TryRecoverAsync(
                    account.Id,
                    currentRecoveryCodeHash,
                    newPasswordHash,
                    newRecoveryCodeHash,
                    cancellationToken);

        if (!recovered)
        {
            return Failure(
                GenericRecoveryFailureMessage);
        }

        return new OwnerPasswordRecoveryResult(
            true,
            NewRecoveryCode:
                newRecoveryCode);
    }

    private static OwnerPasswordRecoveryResult Failure(
        string message)
    {
        return new OwnerPasswordRecoveryResult(
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
