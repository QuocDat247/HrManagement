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

            AuthenticationResult result = isValid
                ? new AuthenticationResult(true)
                : new AuthenticationResult(
                    false,
                    "Tên đăng nhập hoặc mật khẩu không đúng.");

            return Task.FromResult(result);
        }
    }
}