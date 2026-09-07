namespace HrManagement.Application.Authentication.Credentials;

public interface IPasswordBlocklist
{
    bool IsBlocked(
        string normalizedPassword);
}
