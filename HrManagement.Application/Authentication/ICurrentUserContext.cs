namespace HrManagement.Application.Authentication;

public interface ICurrentUserContext
{
    AuthenticatedUser? CurrentUser
    {
        get;
    }

    bool IsAuthenticated
    {
        get;
    }
}
