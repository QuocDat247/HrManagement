using HrManagement.Application.Authentication;
using HrManagement.Infrastructure.Authentication;

namespace HrManagement.Tests.Authentication;

public sealed class CurrentUserSessionTests
{
    [Fact]
    public void SignOut_AfterSignIn_ClearsCurrentUser()
    {
        var session =
            new CurrentUserSession();

        session.SignIn(
            new AuthenticatedUser(
                UserId: "demo-admin",
                Username: "admin",
                DisplayName: "Quản trị viên"));

        Assert.True(
            session.IsAuthenticated);

        Assert.NotNull(
            session.CurrentUser);

        session.SignOut();

        Assert.False(
            session.IsAuthenticated);

        Assert.Null(
            session.CurrentUser);
    }
}
