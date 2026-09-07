using HrManagement.Application.Authentication.Security;
using HrManagement.Domain.Authentication.Security;
using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authentication.Credentials;
using HrManagement.Domain.Authentication.Credentials;

namespace HrManagement.Application.Authentication;

public sealed class RealAuthenticationService
    : IAuthenticationService
{
    private const string InvalidCredentialsMessage =
        "Tên đăng nhập hoặc mật khẩu không đúng.";

    private const int LockoutFailureThreshold =
        5;

    private static readonly TimeSpan LockoutDuration =
        TimeSpan.FromMinutes(
            15);

    private readonly IUserLoginSecurityStateRepository
        _securityStateRepository;

    private readonly TimeProvider
        _timeProvider;

    private readonly IUserAccountRepository
        _accountRepository;

    private readonly IUserCredentialRepository
        _credentialRepository;

    private readonly IPasswordHasher
        _passwordHasher;

    private readonly IUserSession
        _userSession;

    public RealAuthenticationService(
        IUserAccountRepository accountRepository,
        IUserCredentialRepository credentialRepository,
        IUserLoginSecurityStateRepository securityStateRepository,
        IPasswordHasher passwordHasher,
        IUserSession userSession,
        TimeProvider timeProvider)
    {
        _securityStateRepository =
            securityStateRepository;

        _timeProvider =
            timeProvider;

        _accountRepository =
            accountRepository;

        _credentialRepository =
            credentialRepository;

        _passwordHasher =
            passwordHasher;

        _userSession =
            userSession;
    }

    public async Task<AuthenticationResult> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _userSession.SignOut();

        if (string.IsNullOrWhiteSpace(
                username)
            || string.IsNullOrEmpty(
                password))
        {
            return InvalidCredentials();
        }

        var account =
            await _accountRepository
                .GetByUsernameAsync(
                    username,
                    cancellationToken);

        if (account is null
            || !account.IsActive)
        {
            return InvalidCredentials();
        }

        DateTimeOffset nowUtc =
            _timeProvider.GetUtcNow();

        UserLoginSecurityState? securityState =
            await _securityStateRepository
                .GetByAccountIdAsync(
                    account.Id,
                    cancellationToken);

        if (securityState?.IsLockedOut(
                nowUtc) == true)
        {
            return InvalidCredentials();
        }

        bool hadExpiredLockout =
            securityState?.LockoutEndUtc is not null
            && !securityState.IsLockedOut(
                nowUtc);

        UserCredential? credential =
            await _credentialRepository
                .GetByAccountIdAsync(
                    account.Id,
                    cancellationToken);

        if (credential is null)
        {
            return InvalidCredentials();
        }

        PasswordVerificationResult
            verificationResult =
                _passwordHasher.VerifyPassword(
                    password,
                    credential.PasswordHash);

        if (verificationResult ==
            PasswordVerificationResult.Failed)
        {
            int currentFailureCount =
                hadExpiredLockout
                    ? 0
                    : securityState?
                        .FailedLoginCount
                        ?? 0;

            int nextFailureCount =
                currentFailureCount + 1;

            DateTimeOffset? lockoutEndUtc =
                nextFailureCount >=
                    LockoutFailureThreshold
                    ? nowUtc.Add(
                        LockoutDuration)
                    : null;

            var updatedSecurityState =
                new UserLoginSecurityState(
                    account.Id,
                    nextFailureCount,
                    lockoutEndUtc);

            if (securityState is null)
            {
                await _securityStateRepository
                    .AddAsync(
                        updatedSecurityState,
                        cancellationToken);
            }
            else
            {
                await _securityStateRepository
                    .UpdateAsync(
                        updatedSecurityState,
                        cancellationToken);
            }

            return InvalidCredentials();
        }

        if (verificationResult ==
            PasswordVerificationResult
                .SuccessRehashNeeded)
        {
            string upgradedPasswordHash =
                _passwordHasher.HashPassword(
                    password);

            credential =
                new UserCredential(
                    account.Id,
                    upgradedPasswordHash,
                    credential.MustChangePassword);

            await _credentialRepository
                .UpdateAsync(
                    credential,
                    cancellationToken);
        }

        if (securityState is not null
            && (securityState.FailedLoginCount > 0
                || securityState.LockoutEndUtc
                    is not null))
        {
            await _securityStateRepository
                .UpdateAsync(
                    new UserLoginSecurityState(
                        account.Id),
                    cancellationToken);
        }

        _userSession.SignIn(
            new AuthenticatedUser(
                UserId:
                    account.Id.ToString("D"),
                Username:
                    account.Username,
                DisplayName:
                    account.DisplayName));

        return new AuthenticationResult(
            true,
            MustChangePassword:
                credential.MustChangePassword);
    }

    private static AuthenticationResult
        InvalidCredentials()
    {
        return new AuthenticationResult(
            false,
            InvalidCredentialsMessage);
    }
}
