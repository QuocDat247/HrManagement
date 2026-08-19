namespace HrManagement.Application.Authentication;

public interface IUserSession
    : ICurrentUserContext
{
    void SignIn(
        AuthenticatedUser user);

    void SignOut();
}
