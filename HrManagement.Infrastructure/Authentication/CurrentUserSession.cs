using HrManagement.Application.Authentication;

namespace HrManagement.Infrastructure.Authentication;

public sealed class CurrentUserSession
    : IUserSession
{
    public AuthenticatedUser? CurrentUser
    {
        get;
        private set;
    }

    public bool IsAuthenticated =>
        CurrentUser is not null;

    public void SignIn(
        AuthenticatedUser user)
    {
        ArgumentNullException.ThrowIfNull(
            user);

        CurrentUser =
            user;
    }

    public void SignOut()
    {
        CurrentUser =
            null;
    }
}
