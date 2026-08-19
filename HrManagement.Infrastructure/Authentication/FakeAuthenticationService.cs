using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HrManagement.Application.Authentication;

namespace HrManagement.Infrastructure.Authentication
{
    public sealed class FakeAuthenticationService : IAuthenticationService
    {
        private const string DemoUsername = "admin";
        private const string DemoPassword = "admin123";

        private readonly IUserSession _userSession;

        public FakeAuthenticationService(IUserSession userSession)
        {
            _userSession = userSession;
        }

        public Task<AuthenticationResult> LoginAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string normalizedUsername = username.Trim();

            bool isValid =
                string.Equals(
                    normalizedUsername,
                    DemoUsername,
                    StringComparison.OrdinalIgnoreCase)
                && password == DemoPassword;

            if (!isValid)
            {
                return Task.FromResult(
                    new AuthenticationResult(
                        false,
                        "Tên đăng nhập hoặc mật khẩu không đúng."));
            }

            _userSession.SignIn(
                new AuthenticatedUser(
                    UserId: "demo-admin",
                    Username: DemoUsername,
                    DisplayName: "Quản trị viên"));

            return Task.FromResult(
                new AuthenticationResult(
                    true));
        }
    }
}
