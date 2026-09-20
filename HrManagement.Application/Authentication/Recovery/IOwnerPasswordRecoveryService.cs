namespace HrManagement.Application.Authentication.Recovery;

public interface IOwnerPasswordRecoveryService
{
    Task<OwnerPasswordRecoveryResult> RecoverAsync(
        OwnerPasswordRecoveryRequest request,
        CancellationToken cancellationToken = default);
}
