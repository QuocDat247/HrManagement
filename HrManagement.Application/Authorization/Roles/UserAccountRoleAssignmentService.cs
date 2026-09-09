using HrManagement.Application.Authentication;
using HrManagement.Domain.Authorization.Roles;

namespace HrManagement.Application.Authorization.Roles;

public sealed class UserAccountRoleAssignmentService
    : IUserAccountRoleAssignmentService
{
    private readonly ICurrentUserContext
        _currentUserContext;

    private readonly IUserAccountRoleAssignmentPersistence
        _persistence;

    public UserAccountRoleAssignmentService(
        ICurrentUserContext currentUserContext,
        IUserAccountRoleAssignmentPersistence persistence)
    {
        _currentUserContext =
            currentUserContext;

        _persistence =
            persistence;
    }

    public async Task<AccountRoleAssignmentResult> ReplaceAsync(
        ReplaceAccountRolesRequest request,
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

        if (request.RoleIds is null)
        {
            return Failure(
                "Danh sách vai trò không hợp lệ.");
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

        var assignmentsByRoleId =
            new Dictionary<Guid, UserAccountRole>();

        try
        {
            foreach (Guid roleId
                     in request.RoleIds)
            {
                var assignment =
                    new UserAccountRole(
                        request.AccountId,
                        roleId);

                assignmentsByRoleId[
                    assignment.RoleId] =
                        assignment;
            }
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }

        UserAccountRole[] assignments =
            assignmentsByRoleId
                .Values
                .OrderBy(
                    assignment =>
                        assignment.RoleId)
                .ToArray();

        UserAccountRoleAssignmentPersistenceResult result =
            await _persistence
                .TryReplaceAsync(
                    actorAccountId,
                    request.AccountId,
                    assignments,
                    cancellationToken);

        return result switch
        {
            UserAccountRoleAssignmentPersistenceResult.Updated =>
                new AccountRoleAssignmentResult(
                    true,
                    request.AccountId),

            UserAccountRoleAssignmentPersistenceResult.Unchanged =>
                new AccountRoleAssignmentResult(
                    true,
                    request.AccountId),

            UserAccountRoleAssignmentPersistenceResult.AccountNotFound =>
                Failure(
                    "Không tìm thấy tài khoản."),

            UserAccountRoleAssignmentPersistenceResult
                .OwnerAccountNotSupported =>
                    Failure(
                        "Owner không sử dụng vai trò phân quyền."),

            UserAccountRoleAssignmentPersistenceResult.RoleNotFound =>
                Failure(
                    "Không tìm thấy một hoặc nhiều vai trò."),

            UserAccountRoleAssignmentPersistenceResult
                .ActorNotAuthorized =>
                    Failure(
                        "Bạn không có quyền thay đổi vai trò của tài khoản."),

            UserAccountRoleAssignmentPersistenceResult
                .PermissionEscalation =>
                    Failure(
                        "Không thể gán vai trò có quyền vượt quá phạm vi quyền hiện có."),

            _ =>
                Failure(
                    "Không thể cập nhật vai trò của tài khoản.")
        };
    }

    private static AccountRoleAssignmentResult Failure(
        string message)
    {
        return new AccountRoleAssignmentResult(
            false,
            ErrorMessage:
                message);
    }
}
