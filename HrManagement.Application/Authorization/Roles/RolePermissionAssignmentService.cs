using HrManagement.Application.Authentication;
using HrManagement.Domain.Authorization.Roles;

namespace HrManagement.Application.Authorization.Roles;

public sealed class RolePermissionAssignmentService
    : IRolePermissionAssignmentService
{
    private readonly ICurrentUserContext
        _currentUserContext;

    private readonly IRolePermissionAssignmentPersistence
        _persistence;

    public RolePermissionAssignmentService(
        ICurrentUserContext currentUserContext,
        IRolePermissionAssignmentPersistence persistence)
    {
        _currentUserContext =
            currentUserContext;

        _persistence =
            persistence;
    }

    public async Task<RoleManagementResult> ReplaceAsync(
        Guid roleId,
        IReadOnlyCollection<string> permissionCodes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            permissionCodes);

        cancellationToken
            .ThrowIfCancellationRequested();

        if (roleId == Guid.Empty)
        {
            return Failure(
                "Vai trò không hợp lệ.");
        }

        if (!_currentUserContext.IsAuthenticated
            || _currentUserContext.CurrentUser is null
            || !Guid.TryParse(
                _currentUserContext.CurrentUser.UserId,
                out Guid actorAccountId))
        {
            return Failure(
                "Phiên đăng nhập không hợp lệ.");
        }

        var normalizedPermissions =
            new Dictionary<string, RolePermission>(
                StringComparer.Ordinal);

        try
        {
            foreach (string permissionCode
                     in permissionCodes)
            {
                var permission =
                    new RolePermission(
                        roleId,
                        permissionCode);

                normalizedPermissions[
                    permission.PermissionCode] =
                        permission;
            }
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }

        RolePermission[] permissions =
            normalizedPermissions
                .Values
                .OrderBy(
                    permission =>
                        permission.PermissionCode,
                    StringComparer.Ordinal)
                .ToArray();

        RolePermissionAssignmentPersistenceResult result =
            await _persistence
                .TryReplaceAsync(
                    actorAccountId,
                    roleId,
                    permissions,
                    cancellationToken);

        return result switch
        {
            RolePermissionAssignmentPersistenceResult.Updated =>
                new RoleManagementResult(
                    true,
                    roleId),

            RolePermissionAssignmentPersistenceResult.Unchanged =>
                new RoleManagementResult(
                    true,
                    roleId),

            RolePermissionAssignmentPersistenceResult.RoleNotFound =>
                Failure(
                    "Không tìm thấy vai trò."),

            RolePermissionAssignmentPersistenceResult.ActorNotAuthorized =>
                Failure(
                    "Bạn không có quyền thay đổi quyền của vai trò."),

            RolePermissionAssignmentPersistenceResult.PermissionEscalation =>
                Failure(
                    "Không thể cấp cho vai trò quyền vượt quá phạm vi quyền hiện có."),

            _ =>
                Failure(
                    "Không thể cập nhật quyền của vai trò.")
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
