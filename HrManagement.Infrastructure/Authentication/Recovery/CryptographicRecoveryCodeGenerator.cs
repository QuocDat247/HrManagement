using HrManagement.Application.Authentication.Recovery;
using System.Security.Cryptography;

namespace HrManagement.Infrastructure.Authentication.Recovery;

public sealed class CryptographicRecoveryCodeGenerator
    : IRecoveryCodeGenerator
{
    private const int RecoveryCodeByteLength =
        16;

    public string Generate()
    {
        byte[] bytes =
            RandomNumberGenerator.GetBytes(
                RecoveryCodeByteLength);

        string normalized =
            Convert.ToHexString(
                bytes);

        return RecoveryCodeFormat.Format(
            normalized);
    }
}
