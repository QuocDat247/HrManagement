namespace HrManagement.Application.Authentication.Accounts;

public sealed class AccountProfileUpdateService
    : IAccountProfileUpdateService
{
    private readonly IAccountProfileUpdatePersistence
        _persistence;

    public AccountProfileUpdateService(
        IAccountProfileUpdatePersistence persistence)
    {
        _persistence =
            persistence;
    }

    public async Task<UpdateAccountProfileResult> UpdateAsync(
        UpdateAccountProfileRequest request,
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

        if (string.IsNullOrWhiteSpace(
                request.DisplayName))
        {
            return Failure(
                "Vui lòng nhập tên hiển thị.");
        }

        string displayName =
            request.DisplayName.Trim();

        if (displayName.Length > 200)
        {
            return Failure(
                "Tên hiển thị không được vượt quá 200 ký tự.");
        }

        if (request.EmployeeId == Guid.Empty)
        {
            return Failure(
                "Nhân viên liên kết không hợp lệ.");
        }

        AccountProfileUpdatePersistenceResult result =
            await _persistence
                .TryUpdateAsync(
                    request.AccountId,
                    displayName,
                    request.EmployeeId,
                    cancellationToken);

        return result switch
        {
            AccountProfileUpdatePersistenceResult.Updated =>
                new UpdateAccountProfileResult(
                    true),

            AccountProfileUpdatePersistenceResult.AccountNotFound =>
                Failure(
                    "Không tìm thấy tài khoản."),

            AccountProfileUpdatePersistenceResult.EmployeeNotFound =>
                Failure(
                    "Không tìm thấy nhân viên liên kết."),

            AccountProfileUpdatePersistenceResult.EmployeeAlreadyLinked =>
                Failure(
                    "Nhân viên này đã được liên kết với tài khoản khác."),

            _ =>
                Failure(
                    "Không thể cập nhật tài khoản.")
        };
    }

    private static UpdateAccountProfileResult Failure(
        string message)
    {
        return new UpdateAccountProfileResult(
            false,
            message);
    }
}
