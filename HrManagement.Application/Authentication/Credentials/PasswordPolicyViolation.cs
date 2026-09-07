namespace HrManagement.Application.Authentication.Credentials;

public enum PasswordPolicyViolation
{
    None = 0,
    TooShort = 1,
    TooLong = 2,
    Blocked = 3
}
