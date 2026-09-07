namespace HrManagement.Application.Authentication.Credentials;

public interface IPasswordPolicy
{
    PasswordPolicyResult Evaluate(
        string password,
        string? username = null);
}
