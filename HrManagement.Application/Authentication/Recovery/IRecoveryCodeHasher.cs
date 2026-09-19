namespace HrManagement.Application.Authentication.Recovery;

public interface IRecoveryCodeHasher
{
    string Hash(
        string recoveryCode);

    bool Verify(
        string recoveryCode,
        string recoveryCodeHash);
}
