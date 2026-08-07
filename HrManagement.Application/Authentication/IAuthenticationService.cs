using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HrManagement.Application.Authentication
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> LoginAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default);
    }
}
