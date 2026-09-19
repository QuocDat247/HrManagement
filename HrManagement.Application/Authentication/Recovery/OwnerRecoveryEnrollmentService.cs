using HrManagement.Application.Authentication;
using HrManagement.Application.Authentication.Accounts;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Recovery;

namespace HrManagement.Application.Authentication.Recovery;

public sealed class OwnerRecoveryEnrollmentService
    : IOwnerRecoveryEnrollmentService
{
    private readonly ICurrentUserContext
        _currentUserContext;

    private readonly IUserAccountRepository
        _accountRepository;

    private readonly IRecoveryCodeGenerator
        _recoveryCodeGenerator;

    private readonly IRecoveryCodeHasher
        _recoveryCodeHasher;

    private readonly IOwnerRecoveryEnrollmentPersistence
        _persistence;

    public OwnerRecoveryEnrollmentService(
        ICurrentUserContext currentUserContext,
        IUserAccountRepository accountRepository,
        IRecoveryCodeGenerator recoveryCodeGenerator,
        IRecoveryCodeHasher recoveryCodeHasher,
        IOwnerRecoveryEnrollmentPersistence persistence)
    {
        _currentUserContext =
            currentUserContext;

        _accountRepository =
            accountRepository;

        _recoveryCodeGenerator =
            recoveryCodeGenerator;

        _recoveryCodeHasher =
            recoveryCodeHasher;

        _persistence =
            persistence;
    }

    public async Task<bool> IsEnrollmentRequiredAsync(
        CancellationToken cancellationToken = default)
    {
        UserAccount? owner =
            await GetCurrentActiveOwnerAsync(
                cancellationToken);

        if (owner is null)
        {
            return false;
        }

        bool exists =
            await _persistence.ExistsAsync(
                owner.Id,
                cancellationToken);

        return !exists;
    }

    public string GenerateRecoveryCode()
    {
        return _recoveryCodeGenerator.Generate();
    }

    public async Task<OwnerRecoveryEnrollmentResult>
        EnrollAsync(
            string recoveryCode,
            CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        if (RecoveryCodeFormat.Normalize(
                recoveryCode) is null)
        {
            return Failure(
                "Mã khôi phục không hợp lệ.");
        }

        UserAccount? owner =
            await GetCurrentActiveOwnerAsync(
                cancellationToken);

        if (owner is null)
        {
            return Failure(
                "Chỉ Owner đang hoạt động mới có thể thiết lập mã khôi phục.");
        }

        bool alreadyExists =
            await _persistence.ExistsAsync(
                owner.Id,
                cancellationToken);

        if (alreadyExists)
        {
            return Failure(
                "Tài khoản Owner đã có mã khôi phục.");
        }

        string recoveryCodeHash =
            _recoveryCodeHasher.Hash(
                recoveryCode);

        var credential =
            new OwnerRecoveryCredential(
                owner.Id,
                recoveryCodeHash);

        bool created =
            await _persistence.TryCreateAsync(
                credential,
                cancellationToken);

        if (!created)
        {
            return Failure(
                "Tài khoản Owner đã có mã khôi phục.");
        }

        return new OwnerRecoveryEnrollmentResult(
            true);
    }

    private async Task<UserAccount?>
        GetCurrentActiveOwnerAsync(
            CancellationToken cancellationToken)
    {
        AuthenticatedUser? currentUser =
            _currentUserContext.CurrentUser;

        if (!_currentUserContext.IsAuthenticated
            || currentUser is null
            || !Guid.TryParse(
                currentUser.UserId,
                out Guid accountId))
        {
            return null;
        }

        UserAccount? account =
            await _accountRepository
                .GetByIdAsync(
                    accountId,
                    cancellationToken);

        if (account is null
            || !account.IsActive
            || account.Kind !=
                UserAccountKind.Owner)
        {
            return null;
        }

        return account;
    }

    private static OwnerRecoveryEnrollmentResult Failure(
        string message)
    {
        return new OwnerRecoveryEnrollmentResult(
            false,
            message);
    }
}
