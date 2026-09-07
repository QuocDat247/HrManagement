namespace HrManagement.Application.Authentication.Credentials;

public interface IPasswordHasher
{
    string HashPassword(
        string password);

    PasswordVerificationResult VerifyPassword(
        string password,
        string passwordHash);
}
