namespace HrManagement.Application.Authentication.Accounts;

public sealed class AccountActiveStateService
    : IAccountActiveStateService
{
    private readonly IAccountActiveStatePersistence
        _persistence;

    public AccountActiveStateService(
        IAccountActiveStatePersistence persistence)
    {
        _persistence =
            persistence;
    }

    public async Task<SetAccountActiveStateResult> SetAsync(
        SetAccountActiveStateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        cancellationToken
            .ThrowIfCancellationRequested();

        if (request.AccountId == Guid.Empty)
        {
            return Failure(
                "Tài khoản không hợp lệ.");
        }

        AccountActiveStatePersistenceResult result =
            await _persistence
                .TrySetAsync(
                    request.AccountId,
                    request.IsActive,
                    cancellationToken);

        return result switch
        {
            AccountActiveStatePersistenceResult.Updated =>
                new SetAccountActiveStateResult(
                    true),

            AccountActiveStatePersistenceResult.Unchanged =>
                new SetAccountActiveStateResult(
                    true),

            AccountActiveStatePersistenceResult.AccountNotFound =>
                Failure(
                    "Không tìm thấy tài khoản."),

            AccountActiveStatePersistenceResult.LastActiveOwner =>
                Failure(
                    "Không thể vô hiệu hóa Owner đang hoạt động cuối cùng."),

            _ =>
                Failure(
                    "Không thể thay đổi trạng thái tài khoản.")
        };
    }

    private static SetAccountActiveStateResult Failure(
        string message)
    {
        return new SetAccountActiveStateResult(
            false,
            message);
    }
}
