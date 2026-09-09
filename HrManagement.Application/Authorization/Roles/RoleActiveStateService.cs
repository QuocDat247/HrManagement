namespace HrManagement.Application.Authorization.Roles;

public sealed class RoleActiveStateService
    : IRoleActiveStateService
{
    private readonly IRoleActiveStatePersistence
        _persistence;

    public RoleActiveStateService(
        IRoleActiveStatePersistence persistence)
    {
        _persistence =
            persistence;
    }

    public async Task<RoleManagementResult> SetAsync(
        Guid roleId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        if (roleId == Guid.Empty)
        {
            return Failure(
                "Vai trò không hợp lệ.");
        }

        RoleActiveStatePersistenceResult result =
            await _persistence
                .TrySetAsync(
                    roleId,
                    isActive,
                    cancellationToken);

        return result switch
        {
            RoleActiveStatePersistenceResult.Updated =>
                new RoleManagementResult(
                    true,
                    roleId),

            RoleActiveStatePersistenceResult.Unchanged =>
                new RoleManagementResult(
                    true,
                    roleId),

            RoleActiveStatePersistenceResult.RoleNotFound =>
                Failure(
                    "Không tìm thấy vai trò."),

            _ =>
                Failure(
                    "Không thể thay đổi trạng thái vai trò.")
        };
    }

    private static RoleManagementResult Failure(
        string message)
    {
        return new RoleManagementResult(
            false,
            ErrorMessage:
                message);
    }
}
