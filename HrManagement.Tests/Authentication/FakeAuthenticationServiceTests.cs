using HrManagement.Application.Authentication;
using HrManagement.Infrastructure.Authentication;

namespace HrManagement.Tests.Authentication;

public sealed class FakeAuthenticationServiceTests
{
    private readonly CurrentUserSession
        _userSession =
            new();

    private readonly FakeAuthenticationService
        _service;

    public FakeAuthenticationServiceTests()
    {
        _service =
            new FakeAuthenticationService(
                _userSession);
    }

    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ReturnsSuccess()
    {
        AuthenticationResult result =
            await _service.LoginAsync(
                "admin",
                "admin123");

        Assert.True(
            result.IsSuccessful);

        Assert.Null(
            result.ErrorMessage);

        Assert.True(
            _userSession.IsAuthenticated);

        Assert.NotNull(
            _userSession.CurrentUser);

        Assert.Equal(
            "demo-admin",
            _userSession.CurrentUser!.UserId);

        Assert.Equal(
            "admin",
            _userSession.CurrentUser.Username);

        Assert.Equal(
            "Quản trị viên",
            _userSession.CurrentUser.DisplayName);
    }

    [Theory]
    [InlineData("ADMIN")]
    [InlineData("Admin")]
    [InlineData(" admin ")]
    public async Task LoginAsync_WithDifferentUsernameCasing_ReturnsSuccess(
        string username)
    {
        AuthenticationResult result =
            await _service.LoginAsync(
                username,
                "admin123");

        Assert.True(
            result.IsSuccessful);

        Assert.True(
            _userSession.IsAuthenticated);

        Assert.NotNull(
            _userSession.CurrentUser);

        Assert.Equal(
            "admin",
            _userSession.CurrentUser!.Username);
    }

    [Theory]
    [InlineData("admin", "wrong-password")]
    [InlineData("wrong-user", "admin123")]
    [InlineData("", "")]
    public async Task LoginAsync_WithInvalidCredentials_ReturnsFailure(
        string username,
        string password)
    {
        AuthenticationResult result =
            await _service.LoginAsync(
                username,
                password);

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.ErrorMessage));

        Assert.False(
            _userSession.IsAuthenticated);

        Assert.Null(
            _userSession.CurrentUser);
    }
}
