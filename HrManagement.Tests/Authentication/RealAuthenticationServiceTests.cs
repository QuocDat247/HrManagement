using HrManagement.Application.Authentication;
using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authentication.Credentials;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Credentials;
using HrManagement.Infrastructure.Authentication;

namespace HrManagement.Tests.Authentication;

public sealed class RealAuthenticationServiceTests
{
    private const string CorrectPassword =
        "Correct password 123";

    private const string CurrentPasswordHash =
        "$test$current$hash";

    private const string RehashNeededPasswordHash =
        "$test$rehash-needed$hash";

    private const string UpgradedPasswordHash =
        "$test$upgraded$hash";

    [Fact]
    public async Task
        LoginAsync_WithValidCredentials_SignsIn()
    {
        UserAccount account =
            CreateAccount();

        var accountRepository =
            new TestUserAccountRepository(
                account);

        var credentialRepository =
            new TestUserCredentialRepository(
                new UserCredential(
                    account.Id,
                    CurrentPasswordHash,
                    mustChangePassword: true));

        var userSession =
            new CurrentUserSession();

        var service =
            new RealAuthenticationService(
                accountRepository,
                credentialRepository,
                new TestPasswordHasher(),
                userSession);

        AuthenticationResult result =
            await service.LoginAsync(
                " OWNER ",
                CorrectPassword);

        Assert.True(
            result.IsSuccessful);

        Assert.Null(
            result.ErrorMessage);

        Assert.True(
            result.MustChangePassword);

        Assert.True(
            userSession.IsAuthenticated);

        Assert.NotNull(
            userSession.CurrentUser);

        Assert.Equal(
            account.Id.ToString("D"),
            userSession.CurrentUser!.UserId);

        Assert.Equal(
            account.Username,
            userSession.CurrentUser.Username);

        Assert.Equal(
            account.DisplayName,
            userSession.CurrentUser.DisplayName);
    }

    [Fact]
    public async Task
        LoginAsync_WithWrongPassword_ReturnsFailure()
    {
        UserAccount account =
            CreateAccount();

        var userSession =
            new CurrentUserSession();

        var service =
            new RealAuthenticationService(
                new TestUserAccountRepository(
                    account),
                new TestUserCredentialRepository(
                    new UserCredential(
                        account.Id,
                        CurrentPasswordHash)),
                new TestPasswordHasher(),
                userSession);

        AuthenticationResult result =
            await service.LoginAsync(
                account.Username,
                "Wrong password 123");

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Tên đăng nhập hoặc mật khẩu không đúng.",
            result.ErrorMessage);

        Assert.False(
            userSession.IsAuthenticated);
    }

    [Fact]
    public async Task
        LoginAsync_WithUnknownUsername_ReturnsFailure()
    {
        var userSession =
            new CurrentUserSession();

        var service =
            new RealAuthenticationService(
                new TestUserAccountRepository(
                    account: null),
                new TestUserCredentialRepository(
                    credential: null),
                new TestPasswordHasher(),
                userSession);

        AuthenticationResult result =
            await service.LoginAsync(
                "unknown-user",
                CorrectPassword);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Tên đăng nhập hoặc mật khẩu không đúng.",
            result.ErrorMessage);

        Assert.False(
            userSession.IsAuthenticated);
    }

    [Fact]
    public async Task
        LoginAsync_WithInactiveAccount_ReturnsFailure()
    {
        UserAccount account =
            CreateAccount(
                isActive: false);

        var userSession =
            new CurrentUserSession();

        var service =
            new RealAuthenticationService(
                new TestUserAccountRepository(
                    account),
                new TestUserCredentialRepository(
                    new UserCredential(
                        account.Id,
                        CurrentPasswordHash)),
                new TestPasswordHasher(),
                userSession);

        AuthenticationResult result =
            await service.LoginAsync(
                account.Username,
                CorrectPassword);

        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            "Tên đăng nhập hoặc mật khẩu không đúng.",
            result.ErrorMessage);

        Assert.False(
            userSession.IsAuthenticated);
    }

    [Fact]
    public async Task
        LoginAsync_WithMissingCredential_ReturnsFailure()
    {
        UserAccount account =
            CreateAccount();

        var userSession =
            new CurrentUserSession();

        var service =
            new RealAuthenticationService(
                new TestUserAccountRepository(
                    account),
                new TestUserCredentialRepository(
                    credential: null),
                new TestPasswordHasher(),
                userSession);

        AuthenticationResult result =
            await service.LoginAsync(
                account.Username,
                CorrectPassword);

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            userSession.IsAuthenticated);
    }

    [Fact]
    public async Task
        LoginAsync_WhenRehashNeeded_UpdatesCredential()
    {
        UserAccount account =
            CreateAccount();

        var credentialRepository =
            new TestUserCredentialRepository(
                new UserCredential(
                    account.Id,
                    RehashNeededPasswordHash,
                    mustChangePassword: false));

        var service =
            new RealAuthenticationService(
                new TestUserAccountRepository(
                    account),
                credentialRepository,
                new TestPasswordHasher(),
                new CurrentUserSession());

        AuthenticationResult result =
            await service.LoginAsync(
                account.Username,
                CorrectPassword);

        Assert.True(
            result.IsSuccessful);

        Assert.False(
            result.MustChangePassword);

        Assert.NotNull(
            credentialRepository.UpdatedCredential);

        Assert.Equal(
            UpgradedPasswordHash,
            credentialRepository
                .UpdatedCredential!
                .PasswordHash);

        Assert.False(
            credentialRepository
                .UpdatedCredential
                .MustChangePassword);
    }

    private static UserAccount CreateAccount(
        bool isActive = true)
    {
        return new UserAccount(
            Guid.NewGuid(),
            "owner",
            "Chủ doanh nghiệp",
            UserAccountKind.Owner,
            isActive: isActive);
    }

    private sealed class TestUserAccountRepository
        : IUserAccountRepository
    {
        private readonly UserAccount?
            _account;

        public TestUserAccountRepository(
            UserAccount? account)
        {
            _account =
                account;
        }

        public Task<UserAccount?> GetByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_account is null)
            {
                return Task.FromResult<UserAccount?>(
                    null);
            }

            string normalizedUsername =
                username.Trim()
                    .ToUpperInvariant();

            return Task.FromResult<UserAccount?>(
                normalizedUsername ==
                _account.NormalizedUsername
                    ? _account
                    : null);
        }

        public Task<UserAccount?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<UserAccount?>(
                _account?.Id == id
                    ? _account
                    : null);
        }

        public Task<IReadOnlyList<UserAccount>>
            GetAllAsync(
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<UserAccount> accounts =
                _account is null
                    ? Array.Empty<UserAccount>()
                    : new[]
                    {
                        _account
                    };

            return Task.FromResult(
                accounts);
        }

        public Task AddAsync(
            UserAccount account,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(
            UserAccount account,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestUserCredentialRepository
        : IUserCredentialRepository
    {
        private UserCredential?
            _credential;

        public TestUserCredentialRepository(
            UserCredential? credential)
        {
            _credential =
                credential;
        }

        public UserCredential?
            UpdatedCredential
        {
            get;
            private set;
        }

        public Task<UserCredential?> GetByAccountIdAsync(
            Guid accountId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<UserCredential?>(
                _credential?.AccountId ==
                accountId
                    ? _credential
                    : null);
        }

        public Task AddAsync(
            UserCredential credential,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(
            UserCredential credential,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _credential =
                credential;

            UpdatedCredential =
                credential;

            return Task.CompletedTask;
        }
    }

    private sealed class TestPasswordHasher
        : IPasswordHasher
    {
        public string HashPassword(
            string password)
        {
            return UpgradedPasswordHash;
        }

        public PasswordVerificationResult VerifyPassword(
            string password,
            string passwordHash)
        {
            if (passwordHash ==
                    RehashNeededPasswordHash
                && password ==
                    CorrectPassword)
            {
                return PasswordVerificationResult
                    .SuccessRehashNeeded;
            }

            return passwordHash ==
                    CurrentPasswordHash
                && password ==
                    CorrectPassword
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }
    }
}
