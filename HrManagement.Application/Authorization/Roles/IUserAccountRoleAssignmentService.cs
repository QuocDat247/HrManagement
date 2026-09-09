namespace HrManagement.Application.Authorization.Roles;

public interface IUserAccountRoleAssignmentService
{
    Task<AccountRoleAssignmentResult> ReplaceAsync(
        ReplaceAccountRolesRequest request,
        CancellationToken cancellationToken = default);
}
