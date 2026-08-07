using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HrManagement.Application.Authentication;
using HrManagement.Infrastructure.Authentication;

namespace HrManagement.Tests.Authentication
{
    public sealed class FakeAuthenticationServiceTests
    {
        private readonly FakeAuthenticationService _service = new();

        [Fact]
        public async Task LoginAsync_WithCorrectCredentials_ReturnsSuccess()
        {
            AuthenticationResult result =
                await _service.LoginAsync(
                    "admin",
                    "admin123");

            Assert.True(result.IsSuccessful);
            Assert.Null(result.ErrorMessage);
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

            Assert.True(result.IsSuccessful);
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

            Assert.False(result.IsSuccessful);
            Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }
    }
}
