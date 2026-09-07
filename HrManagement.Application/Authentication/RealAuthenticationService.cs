using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authentication.Credentials;
using HrManagement.Domain.Authentication.Credentials;

namespace HrManagement.Application.Authentication;

public sealed class RealAuthenticationService
    : IAuthenticationService
{
    private const string InvalidCredentialsMessage =
        "Tên đăng nhập hoặc mật khẩu không đúng.";

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
        IPasswordHasher passwordHasher,
        IUserSession userSession)
    {
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
