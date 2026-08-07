using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HrManagement.Application.Authentication
{
    public sealed record AuthenticationResult(
    bool IsSuccessful,
    string? ErrorMessage = null);
}
