using HrManagement.Application.Authentication.Credentials;
using HrManagement.Domain.Authentication.Accounts;
using HrManagement.Domain.Authentication.Credentials;
using HrManagement.Domain.Authentication.Security;

namespace HrManagement.Application.Authentication.Accounts;

public sealed class StandardAccountCreationService
    : IStandardAccountCreationService
{
    private readonly IPasswordPolicy
        _passwordPolicy;

    private readonly IPasswordHasher
        _passwordHasher;

    private readonly IStandardAccountCreationPersistence
        _persistence;

    public StandardAccountCreationService(
        IPasswordPolicy passwordPolicy,
        IPasswordHasher passwordHasher,
        IStandardAccountCreationPersistence persistence)
    {
        _passwordPolicy =
            passwordPolicy;

        _passwordHasher =
            passwordHasher;

        _persistence =
            persistence;
    }

    public async Task<CreateStandardAccountResult> CreateAsync(
        CreateStandardAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        cancellationToken
            .ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(
                request.Username))
        {
            return Failure(
                "Vui lòng nhập tên đăng nhập.");
        }

        if (string.IsNullOrWhiteSpace(
                request.DisplayName))
        {
            return Failure(
                "Vui lòng nhập tên hiển thị.");
        }

        if (request.Password is null)
        {
            return Failure(
                "Vui lòng nhập mật khẩu.");
        }

        UserAccount account;

        try
        {
            account =
                new UserAccount(
                    Guid.NewGuid(),
                    request.Username,
                    request.DisplayName,
                    UserAccountKind.Standard,
                    request.EmployeeId);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }

        PasswordPolicyResult policyResult =
            _passwordPolicy.Evaluate(
                request.Password,
                account.Username);

        if (!policyResult.IsAccepted)
        {
            return Failure(
                GetPasswordPolicyMessage(
                    policyResult.Violation));
        }

        string passwordHash =
            _passwordHasher.HashPassword(
                request.Password);

        var credential =
            new UserCredential(
                account.Id,
                passwordHash,
                mustChangePassword:
                    true);

        var securityState =
            new UserLoginSecurityState(
                account.Id);

        StandardAccountCreationPersistenceResult
            persistenceResult =
                await _persistence
                    .TryCreateAsync(
                        account,
                        credential,
                        securityState,
                        cancellationToken);

        return persistenceResult switch
        {
            StandardAccountCreationPersistenceResult.Created =>
                new CreateStandardAccountResult(
                    true,
                    account.Id),

            StandardAccountCreationPersistenceResult
                .UsernameAlreadyExists =>
                    Failure(
                        "Tên đăng nhập đã tồn tại."),

            StandardAccountCreationPersistenceResult
                .EmployeeNotFound =>
                    Failure(
                        "Không tìm thấy nhân viên liên kết."),

            StandardAccountCreationPersistenceResult
                .EmployeeAlreadyLinked =>
                    Failure(
                        "Nhân viên này đã được liên kết với tài khoản khác."),

            _ =>
                Failure(
                    "Không thể tạo tài khoản.")
        };
    }

    private static CreateStandardAccountResult Failure(
        string message)
    {
        return new CreateStandardAccountResult(
            false,
            ErrorMessage:
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
