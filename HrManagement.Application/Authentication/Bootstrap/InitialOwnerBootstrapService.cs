using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authentication.Credentials;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Credentials;
using HrManagement.Domain.Authentication.Security;

namespace HrManagement.Application.Authentication.Bootstrap;

public sealed class InitialOwnerBootstrapService
    : IInitialOwnerBootstrapService
{
    private readonly IUserAccountRepository
        _accountRepository;

    private readonly IPasswordPolicy
        _passwordPolicy;

    private readonly IPasswordHasher
        _passwordHasher;

    private readonly IInitialOwnerBootstrapPersistence
        _bootstrapPersistence;

    public InitialOwnerBootstrapService(
        IUserAccountRepository accountRepository,
        IPasswordPolicy passwordPolicy,
        IPasswordHasher passwordHasher,
        IInitialOwnerBootstrapPersistence bootstrapPersistence)
    {
        _accountRepository =
            accountRepository;

        _passwordPolicy =
            passwordPolicy;

        _passwordHasher =
            passwordHasher;

        _bootstrapPersistence =
            bootstrapPersistence;
    }

    public async Task<bool> IsBootstrapRequiredAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<UserAccount> accounts =
            await _accountRepository
                .GetAllAsync(
                    cancellationToken);

        return accounts.Count == 0;
    }

    public async Task<OwnerBootstrapResult>
        CreateInitialOwnerAsync(
            string username,
            string displayName,
            string password,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(
                username))
        {
            return Failure(
                "Vui lòng nhập tên đăng nhập.");
        }

        if (string.IsNullOrWhiteSpace(
                displayName))
        {
            return Failure(
                "Vui lòng nhập tên hiển thị.");
        }

        if (password is null)
        {
            return Failure(
                "Vui lòng nhập mật khẩu.");
        }

        IReadOnlyList<UserAccount> existingAccounts =
            await _accountRepository
                .GetAllAsync(
                    cancellationToken);

        if (existingAccounts.Count != 0)
        {
            return Failure(
                "Hệ thống đã được khởi tạo tài khoản.");
        }

        PasswordPolicyResult policyResult =
            _passwordPolicy.Evaluate(
                password,
                username);

        if (!policyResult.IsAccepted)
        {
            return Failure(
                GetPasswordPolicyMessage(
                    policyResult.Violation));
        }

        UserAccount account;

        try
        {
            account =
                new UserAccount(
                    Guid.NewGuid(),
                    username,
                    displayName,
                    UserAccountKind.Owner);
        }
        catch (ArgumentException ex)
        {
            return Failure(
                ex.Message);
        }

        string passwordHash =
            _passwordHasher.HashPassword(
                password);

        var credential =
            new UserCredential(
                account.Id,
                passwordHash,
                mustChangePassword: false);

        var securityState =
            new UserLoginSecurityState(
                account.Id);

        bool created =
            await _bootstrapPersistence
                .TryCreateAsync(
                    account,
                    credential,
                    securityState,
                    cancellationToken);

        if (!created)
        {
            return Failure(
                "Hệ thống đã được khởi tạo tài khoản.");
        }

        return new OwnerBootstrapResult(
            true);
    }

    private static OwnerBootstrapResult Failure(
        string message)
    {
        return new OwnerBootstrapResult(
            false,
            message);
    }

    private static string GetPasswordPolicyMessage(
        PasswordPolicyViolation violation)
    {
        return violation switch
        {
            PasswordPolicyViolation.TooShort =>
                "Mật khẩu quá ngắn.",

            PasswordPolicyViolation.TooLong =>
                "Mật khẩu quá dài.",

            PasswordPolicyViolation.Blocked =>
                "Mật khẩu này không được phép sử dụng.",

            _ =>
                "Mật khẩu không đáp ứng chính sách bảo mật."
        };
    }
}
